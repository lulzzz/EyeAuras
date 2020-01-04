using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using DynamicData;
using DynamicData.Binding;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.Services;
using EyeAuras.UI.MainWindow.Models;
using EyeAuras.UI.Overlay.ViewModels;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity;

namespace EyeAuras.UI.Core.Models
{
    internal sealed class OverlayAuraModelBase : AuraModelBase<OverlayAuraProperties>, IOverlayAuraModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayAuraModelBase));
        private static readonly TimeSpan ModelsReloadTimeout = TimeSpan.FromSeconds(1);
        private static int GlobalAuraIdx;

        private readonly IAuraRepository repository;
        private readonly string defaultAuraName;

        private ICloseController closeController;
        private bool isActive;
        private bool isEnabled = true;
        private string name;
        private WindowMatchParams targetWindow;
        private string uniqueId;

        public OverlayAuraModelBase(
            [NotNull] ISharedContext sharedContext,
            [NotNull] IAuraRepository repository,
            [NotNull] IUniqueIdGenerator idGenerator,
            [NotNull] IFactory<IEyeOverlayViewModel, IOverlayWindowController, IAuraModelController> overlayViewModelFactory,
            [NotNull] IFactory<IOverlayWindowController, IWindowTracker> overlayWindowControllerFactory,
            [NotNull] IFactory<WindowTracker, IStringMatcher> windowTrackerFactory,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            defaultAuraName = $"Aura #{Interlocked.Increment(ref GlobalAuraIdx)}";
            Name = defaultAuraName;
            Id = idGenerator.Next();
            using var sw = new BenchmarkTimer($"[{Name}({Id})] OverlayAuraModel initialization", Log, nameof(OverlayAuraModelBase));
            
            var auraTriggers = new ComplexAuraTrigger();
            Triggers = auraTriggers.Triggers;
            
            var auraActions = new ComplexAuraAction();
            OnEnterActions = auraActions.Actions;
            
            this.repository = repository;
            var matcher = new RegexStringMatcher().AddToWhitelist(".*");
            var windowTracker = windowTrackerFactory
                .Create(matcher)
                .AddTo(Anchors);

            var overlayController = overlayWindowControllerFactory
                .Create(windowTracker)
                .AddTo(Anchors);
            sw.Step($"Overlay controller created: {overlayController}");

            var overlayViewModel = overlayViewModelFactory
                .Create(overlayController, this)
                .AddTo(Anchors);
            sw.Step($"Overlay view model created: {overlayViewModel}");

            Observable.Merge(
                    overlayViewModel.WhenValueChanged(x => x.AttachedWindow, false).ToUnit(),
                    this.WhenValueChanged(x => x.IsActive, false).ToUnit())
                .StartWithDefault()
                .Select(
                    () => new
                    {
                        IsActive,
                        WindowIsAttached = overlayViewModel.AttachedWindow != null
                    })
                .Subscribe(x => overlayController.IsEnabled = x.IsActive && x.WindowIsAttached)
                .AddTo(Anchors);
            
            Overlay = overlayViewModel;
            sw.Step($"Overlay view model initialized: {overlayViewModel}");

            Observable.CombineLatest(
                    auraTriggers.WhenAnyValue(x => x.IsActive),  
                    sharedContext.SystemTrigger.WhenValueChanged(x => x.IsActive))
                .DistinctUntilChanged()
                .Subscribe(x => IsActive = x.All(isActive => isActive), Log.HandleException)
                .AddTo(Anchors);

            auraTriggers.WhenAnyValue(x => x.IsActive)
                .WithPrevious((prev, curr) => new {prev, curr})
                .Where(x => x.prev == false && x.curr)
                .Subscribe(ExecuteOnEnterActions, Log.HandleException)
                .AddTo(Anchors);

            this.repository.KnownEntities
                .ToObservableChangeSet()
                .SkipInitial()
                .Throttle(ModelsReloadTimeout, bgScheduler)
                .ObserveOn(uiScheduler)
                .Subscribe(
                    () =>
                    {
                        var properties = Properties;
                        ReloadCollections(properties);
                    })
                .AddTo(Anchors);
            
            var modelPropertiesToIgnore = new[]
            {
                nameof(IAuraTrigger.IsActive),
                nameof(IAuraTrigger.TriggerDescription),
                nameof(IAuraTrigger.TriggerName),
            }.ToImmutableHashSet();

            //FIXME Properties mechanism should have inverted logic - only important parameters must matter
            Observable.Merge(
                    this.WhenAnyProperty(x => x.Name, x => x.TargetWindow, x => x.IsEnabled).Select(x => $"[{Name}].{x.EventArgs.PropertyName} property changed"),
                    Overlay.WhenAnyProperty().Where(x => !modelPropertiesToIgnore.Contains(x.EventArgs.PropertyName)).Select(x => $"[{Name}].{nameof(Overlay)}.{x.EventArgs.PropertyName} property changed"),
                    Triggers.ToObservableChangeSet().Select(x => $"[{Name}({Id})] Trigger list changed, item count: {Triggers.Count}"),
                    Triggers.ToObservableChangeSet().WhenPropertyChanged().Where(x => !modelPropertiesToIgnore.Contains(x.EventArgs.PropertyName)).Select(x => $"[{Name}].{x.Sender}.{x.EventArgs.PropertyName} Trigger property changed"),
                    OnEnterActions.ToObservableChangeSet().Select(x => $"[{Name}({Id})] Action list changed, item count: {OnEnterActions.Count}"),
                    OnEnterActions.ToObservableChangeSet().WhenPropertyChanged().Where(x => !modelPropertiesToIgnore.Contains(x.EventArgs.PropertyName)).Select(x => $"[{Name}].{x.Sender}.{x.EventArgs.PropertyName} Action property changed"))
                .Subscribe(reason => RaisePropertyChanged(nameof(Properties)))
                .AddTo(Anchors);
            
            Disposable.Create(() =>
            {
                Log.Debug(
                    $"Disposed Aura {Name}({Id}) (aka {defaultAuraName}), triggers: {Triggers.Count}, actions: {OnEnterActions.Count}");
                OnEnterActions.Clear();
                Triggers.Clear();
            }).AddTo(Anchors);
            sw.Step($"Overlay model properties initialized");

            overlayController.RegisterChild(overlayViewModel).AddTo(Anchors);
            sw.Step($"Overlay registration completed: {this}");
        }

        private void ExecuteOnEnterActions()
        {
            Log.Debug($"[{Name}({Id})] Trigger state changed, executing OnEnter Actions");
            OnEnterActions.ForEach(action => action.Execute());
        }

        public bool IsActive
        {
            get => isActive;
            private set => RaiseAndSetIfChanged(ref isActive, value);
        }

        public WindowMatchParams TargetWindow
        {
            get => targetWindow;
            set => RaiseAndSetIfChanged(ref targetWindow, value);
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set => RaiseAndSetIfChanged(ref isEnabled, value);
        }

        public string Id
        {
            get => uniqueId;
            private set => this.RaiseAndSetIfChanged(ref uniqueId, value);
        }

        public ObservableCollection<IAuraTrigger> Triggers { get; }

        public ObservableCollection<IAuraAction> OnEnterActions { get; }

        public IEyeOverlayViewModel Overlay { get; }

        public void SetCloseController(ICloseController closeController)
        {
            Guard.ArgumentNotNull(closeController, nameof(closeController));

            CloseController = closeController;
        }

        public ICloseController CloseController
        {
            get => closeController;
            private set => RaiseAndSetIfChanged(ref closeController, value);
        }

        public string Name
        {
            get => name;
            set => RaiseAndSetIfChanged(ref name, value);
        }

        private void ReloadCollections(OverlayAuraProperties source)
        {
            OnEnterActions.Clear();
            Triggers.Clear();
            source.TriggerProperties.Where(ValidateProperty).Select(x => repository.CreateModel<IAuraTrigger>(x)).ForEach(x => Triggers.Add(x));
            source.OnEnterActionProperties.Where(ValidateProperty).Select(x => repository.CreateModel<IAuraAction>(x)).ForEach(x => OnEnterActions.Add(x));
        }

        protected override void Load(OverlayAuraProperties source)
        {
            if (!string.IsNullOrEmpty(source.Id))
            {
                Id = source.Id;
            }
            if (!string.IsNullOrEmpty(source.Name))
            {
                Name = source.Name;
            }

            TargetWindow = source.WindowMatch;
            ReloadCollections(source);
            IsEnabled = source.IsEnabled;
            Overlay.ThumbnailOpacity = source.ThumbnailOpacity;
            Overlay.Region.SetValue(source.SourceRegionBounds);
            Overlay.IsClickThrough = source.IsClickThrough;
            Overlay.MaintainAspectRatio = source.MaintainAspectRatio;
            Overlay.BorderColor = source.BorderColor;
            Overlay.BorderThickness = source.BorderThickness;

            var bounds = source.OverlayBounds.ScaleToWpf();
            Overlay.Left = bounds.Left;
            Overlay.Top = bounds.Top;
            Overlay.Height = bounds.Height;
            Overlay.Width = bounds.Width;
        }

        protected override OverlayAuraProperties Save()
        {
            var save = new OverlayAuraProperties
            {
                Name = Name,
                TriggerProperties = Triggers.Select(x => x.Properties).Where(ValidateProperty).ToList(),
                OnEnterActionProperties = OnEnterActions.Select(x => x.Properties).Where(ValidateProperty).ToList(),
                SourceRegionBounds = Overlay.Region.Bounds,
                OverlayBounds = Overlay.NativeBounds,
                WindowMatch = TargetWindow,
                IsClickThrough = Overlay.IsClickThrough,
                ThumbnailOpacity = Overlay.ThumbnailOpacity,
                MaintainAspectRatio = Overlay.MaintainAspectRatio,
                BorderColor = Overlay.BorderColor,
                BorderThickness = Overlay.BorderThickness,
                IsEnabled = IsEnabled,
                Id = Id,
            };
            return save;
        }

        private bool ValidateProperty(IAuraProperties properties)
        {
            if (properties is EmptyAuraProperties)
            {
                Log.Warn($"[{Name}({Id})] {nameof(EmptyAuraProperties)} should never be used for Models Save/Load purposes - too generic");
                return false;
            }

            return true;
        }
    }
}
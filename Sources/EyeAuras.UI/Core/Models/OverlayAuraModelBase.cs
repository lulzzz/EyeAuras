using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using DynamicData.Binding;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
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

        private readonly string defaultAuraName;
        private readonly IAuraRepository repository;
        private ICloseController closeController;
        private bool isActive;
        private bool isEnabled = true;
        private string name;
        private WindowMatchParams targetWindow;

        public OverlayAuraModelBase(
            [NotNull] IComplexAuraTrigger systemTrigger,
            [NotNull] IAuraRepository repository,
            [NotNull] IFactory<IEyeOverlayViewModel, IOverlayWindowController, IAuraModelController> overlayViewModelFactory,
            [NotNull] IFactory<IOverlayWindowController, IWindowTracker> overlayWindowControllerFactory,
            [NotNull] IFactory<WindowTracker, IStringMatcher> windowTrackerFactory,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            defaultAuraName = $"Aura #{Interlocked.Increment(ref GlobalAuraIdx)}";
            Name = defaultAuraName;

            this.repository = repository;

            var matcher = new RegexStringMatcher().AddToWhitelist(".*");
            var windowTracker = windowTrackerFactory
                .Create(matcher)
                .AddTo(Anchors);

            var overlayController = overlayWindowControllerFactory
                .Create(windowTracker)
                .AddTo(Anchors);

            var overlayViewModel = overlayViewModelFactory
                .Create(overlayController, this)
                .AddTo(Anchors);
            Overlay = overlayViewModel;

            overlayController.RegisterChild(overlayViewModel);
            overlayController.AddTo(Anchors);

            Observable.Merge(
                    Overlay.WhenValueChanged(x => x.AttachedWindow, false).ToUnit(),
                    this.WhenValueChanged(x => x.IsActive, false).ToUnit())
                .Select(
                    () => new
                    {
                        IsActive,
                        WindowIsAttached = Overlay.AttachedWindow != null
                    })
                .Subscribe(x => overlayController.IsEnabled = x.IsActive && x.WindowIsAttached)
                .AddTo(Anchors);

            OnEnterActions = new ObservableCollection<IAuraAction>();

            HiddenTriggers = new ReadOnlyObservableCollection<IAuraTrigger>(systemTrigger.Triggers);

            var auraTriggers = new ComplexAuraTrigger();
            Triggers = auraTriggers.Triggers;

            Observable.Merge(
                    this.WhenValueChanged(x => x.IsEnabled, false).ToUnit(),
                    systemTrigger.WhenAnyValue(x => x.IsActive).ToUnit(),
                    auraTriggers.WhenAnyValue(x => x.IsActive).ToUnit())
                .StartWithDefault()
                .Select(
                    () => new
                    {
                        IsEnabled,
                        TriggersAreActive = auraTriggers.IsActive,
                        SystemTriggerIsActive = systemTrigger.IsActive
                    })
                .DistinctUntilChanged()
                .Subscribe(x => IsActive = x.IsEnabled && x.TriggersAreActive && x.SystemTriggerIsActive, Log.HandleException)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsActive)
                .WithPrevious((prev, curr) => new {prev, curr})
                .Where(x => x.prev == false && x.curr)
                .Subscribe(ExecuteOnEnterActions, Log.HandleException)
                .AddTo(Anchors);

            this.repository.KnownEntities
                .ToObservableChangeSet()
                .Throttle(ModelsReloadTimeout, bgScheduler)
                .ObserveOn(uiScheduler)
                .Subscribe(
                    () =>
                    {
                        var properties = Properties;
                        OnEnterActions.Clear();
                        Triggers.Clear();
                        ReloadTriggers(properties.TriggerProperties);
                        ReloadAction(properties.OnEnterActionProperties);
                    })
                .AddTo(Anchors);
            
            var modelPropertiesToIgnore = new[]
            {
                nameof(IAuraModel.Properties),
                nameof(IAuraTrigger.IsActive),
                nameof(IAuraTrigger.TriggerDescription),
                nameof(IAuraTrigger.TriggerName),
            }.ToImmutableHashSet();

            Observable.Merge(
                    this.WhenAnyProperty().Where(x => !modelPropertiesToIgnore.Contains(x.EventArgs.PropertyName)).Select(x => $"[{Name}].{x.EventArgs.PropertyName} property changed"),
                    Overlay.WhenAnyProperty().Where(x => !modelPropertiesToIgnore.Contains(x.EventArgs.PropertyName)).Select(x => $"[{Name}].{nameof(Overlay)}.{x.EventArgs.PropertyName} property changed"),
                    Triggers.ToObservableChangeSet().Select(x => $"[{Name}] Trigger list changed, item count: {Triggers.Count}"),
                    OnEnterActions.ToObservableChangeSet().Select(x => $"[{Name}] Action list changed, item count: {OnEnterActions.Count}"),
                    Triggers.ToObservableChangeSet().WhenPropertyChanged().Where(x => !modelPropertiesToIgnore.Contains(x.EventArgs.PropertyName)).Select(x => $"[{Name}].{x.Sender}.{x.EventArgs.PropertyName} Trigger property changed"),
                    OnEnterActions.ToObservableChangeSet().WhenPropertyChanged().Where(x => !modelPropertiesToIgnore.Contains(x.EventArgs.PropertyName)).Select(x => $"[{Name}].{x.Sender}.{x.EventArgs.PropertyName} Action property changed"))
                .Subscribe(reason => RaisePropertyChanged(nameof(Properties)))
                .AddTo(Anchors);
        }

        private void ExecuteOnEnterActions()
        {
            Log.Debug($"[{Name}] Trigger state changed, executing OnEnter Actions");
            OnEnterActions.ForEach(action => action.Execute());
        }

        public ReadOnlyObservableCollection<IAuraTrigger> HiddenTriggers { get; }

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

        private void ReloadTriggers(IEnumerable<IAuraProperties> properties)
        {
            Triggers.Clear();
            properties.Where(ValidateProperty).Select(x => repository.CreateModel<IAuraTrigger>(x)).ForEach(x => Triggers.Add(x));
        }

        private void ReloadAction(IEnumerable<IAuraProperties> properties)
        {
            OnEnterActions.Clear();
            properties.Where(ValidateProperty).Select(x => repository.CreateModel<IAuraAction>(x)).ForEach(x => OnEnterActions.Add(x));
        }

        protected override void Load(OverlayAuraProperties source)
        {
            Name = source.Name;
            TargetWindow = source.WindowMatch;
            OnEnterActions.Clear();

            ReloadTriggers(source.TriggerProperties);
            ReloadAction(source.OnEnterActionProperties);

            IsEnabled = source.IsEnabled;
            Overlay.ThumbnailOpacity = source.ThumbnailOpacity;
            Overlay.Region.SetValue(source.SourceRegionBounds);
            Overlay.IsClickThrough = source.IsClickThrough;
            Overlay.MaintainAspectRatio = source.MaintainAspectRatio;
            Overlay.BorderColor = source.BorderColor;
            Overlay.BorderThickness = source.BorderThickness;

            var bounds = source.OverlayBounds.ToWpfRectangle().ScaleToWpf();
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
                IsEnabled = IsEnabled
            };
            return save;
        }

        private bool ValidateProperty(IAuraProperties properties)
        {
            if (properties is EmptyAuraProperties)
            {
                Log.Warn($"[{Name}] {nameof(EmptyAuraProperties)} should never be used for Models Save/Load purposes - too generic");
                return false;
            }

            return true;
        }
    }
}
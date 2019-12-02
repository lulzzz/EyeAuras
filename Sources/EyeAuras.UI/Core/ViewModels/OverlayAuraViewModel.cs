using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.Core.Services;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using Prism.Commands;
using ReactiveUI;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class OverlayAuraViewModel : DisposableReactiveObject, IOverlayAuraViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayAuraViewModel));

        private readonly Fallback<string> tabName = new Fallback<string>();
        private readonly SerialDisposable loadedModelAnchors = new SerialDisposable();
        private readonly IFactory<IOverlayAuraModel> auraModelFactory;
        
        private bool isFlipped;
        private bool isSelected;
        private OverlayAuraProperties properties;
        private bool isEnabled;
        private bool isActive;
        private ICloseController closeController;
        private IOverlayAuraModel model;

        public OverlayAuraViewModel(
            OverlayAuraProperties initialProperties,
            [NotNull] IFactory<IPropertyEditorViewModel> propertiesEditorFactory,
            [NotNull] IFactory<IOverlayAuraModel> auraModelFactory)
        {
            this.auraModelFactory = auraModelFactory;
            loadedModelAnchors.AddTo(Anchors);
            RenameCommand = new DelegateCommand<string>(RenameCommandExecuted);

            this.RaiseWhenSourceValue(x => x.TabName, tabName, x => x.Value).AddTo(Anchors);
            this.RaiseWhenSourceValue(x => x.DefaultTabName, tabName, x => x.DefaultValue).AddTo(Anchors);

            GeneralEditor = propertiesEditorFactory.Create();

            Properties = initialProperties;
            IsEnabled = properties.IsEnabled;
            Id = properties.Id;
            
            tabName.SetValue(properties.Name);
            tabName.SetDefaultValue(properties.Name);
            
            this.WhenAnyValue(x => x.IsEnabled)
                .Subscribe(() => Model = ReloadModel())
                .AddTo(Anchors);
            
            EnableCommand = CommandWrapper.Create(() => IsEnabled = true);
        }

        public string DefaultTabName => tabName.DefaultValue;

        public bool IsActive
        {
            get => isActive;
            private set => this.RaiseAndSetIfChanged(ref isActive, value);
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set => this.RaiseAndSetIfChanged(ref isEnabled, value);
        }

        public ICommand RenameCommand { [NotNull] get; }
        
        public ICommand EnableCommand { get; }

        public IPropertyEditorViewModel GeneralEditor { get; }

        public string TabName => tabName.Value;

        public bool IsFlipped
        {
            get => isFlipped;
            set => RaiseAndSetIfChanged(ref isFlipped, value);
        }

        public bool IsSelected
        {
            get => isSelected;
            set => RaiseAndSetIfChanged(ref isSelected, value);
        }

        public string Id { get; }

        public IOverlayAuraModel Model
        {
            get => model;
            private set => this.RaiseAndSetIfChanged(ref model, value);
        }

        public OverlayAuraProperties Properties
        {
            get => properties;
            private set => this.RaiseAndSetIfChanged(ref properties, value);
        }
        
        public ICloseController CloseController
        {
            get => closeController;
            private set => RaiseAndSetIfChanged(ref closeController, value);
        }
        
        public void SetCloseController(ICloseController closeController)
        {
            Guard.ArgumentNotNull(closeController, nameof(closeController));

            CloseController = closeController;
        }

        private IOverlayAuraModel ReloadModel()
        {
            using var sw = new BenchmarkTimer(isEnabled ? $"[{TabName}({Id})] Loading new model" : $"[{TabName}({Id})] Unloading model", Log, $"{nameof(OverlayAuraViewModel)}.{nameof(ReloadModel)}");

            var modelAnchors = new CompositeDisposable().AssignTo(loadedModelAnchors);
            sw.Step($"Disposed previous model");

            Properties.IsEnabled = isEnabled;
            if (!isEnabled)
            {
                GeneralEditor.Value = null;
                IsActive = false;
                return null;
            }

            var model = auraModelFactory.Create().AddTo(modelAnchors);
            sw.Step($"Created new model: {model}");
            GeneralEditor.Value = model;
            sw.Step($"Initialized model Editor");

            model.Properties = Properties;
            sw.Step($"Loaded model Properties");

            model.WhenAnyValue(x => x.Name)
                .Subscribe(x =>
                {
                    tabName.SetValue(x);
                    tabName.SetDefaultValue(x);
                })
                .AddTo(modelAnchors);
            
            model.WhenAnyValue(x => x.IsActive)
                .Subscribe(modelIsActive => IsActive = modelIsActive)
                .AddTo(modelAnchors);
            
            model.WhenAnyProperty(x => x.Properties)
                .Subscribe(modelProperties => Properties = model.Properties)
                .AddTo(modelAnchors);
            
            model.WhenAnyProperty(x => x.IsEnabled)
                .Subscribe(modelIsEnabled => IsEnabled = model.IsEnabled)
                .AddTo(modelAnchors);
            
            this.WhenAnyValue(x => x.TabName)
                .Subscribe(x => model.Name = TabName)
                .AddTo(modelAnchors);

            this.WhenAnyValue(x => x.CloseController)
                .Where(x => x != null)
                .Subscribe(x => model.SetCloseController(x))
                .AddTo(modelAnchors);
            sw.Step($"Fully initialized model");

            return model;
        }
        
        private void RenameCommandExecuted(string value)
        {
            if (IsFlipped)
            {
                if (value == null)
                {
                    // Cancel
                }
                else if (string.IsNullOrWhiteSpace(value))
                {
                    RenameTabTo(default);
                }
                else
                {
                    RenameTabTo(value);
                }
            }

            IsFlipped = !IsFlipped;
        }

        private void RenameTabTo(string newTabNameOrDefault)
        {
            if (newTabNameOrDefault == tabName.Value)
            {
                return;
            }

            var previousValue = tabName.Value;
            tabName.SetValue(newTabNameOrDefault);
            Log.Debug($"[{TabName}({Id})] Changed name of tab {tabName.DefaultValue}, {previousValue} => {tabName.Value}");
        }

        public override string ToString()
        {
            return new {TabName, DefaultTabName, IsSelected, IsFlipped}.DumpToTextRaw();
        }
    }
}
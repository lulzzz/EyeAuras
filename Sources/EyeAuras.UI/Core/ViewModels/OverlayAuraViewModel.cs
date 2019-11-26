using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using EyeAuras.Shared;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.Overlay.ViewModels;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI;
using Prism.Commands;
using Prism.Modularity;
using ReactiveUI;
using Unity;

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
            tabName.SetValue(properties.Name);
            
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

        private IOverlayAuraModel model;

        public IOverlayAuraModel Model
        {
            get => model;
            set => this.RaiseAndSetIfChanged(ref model, value);
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
            using var unused = new OperationTimer(elapsed => Log.Debug($"[{tabName}] {(isEnabled ? "Model loaded in" : "Model unloaded in")} {elapsed.TotalMilliseconds:F0}ms"));

            var modelAnchors = new CompositeDisposable().AssignTo(loadedModelAnchors);
            if (!isEnabled)
            {
                if (properties != null)
                {
                    properties.IsEnabled = false;
                }
                GeneralEditor.Value = null;
                return null;
            }
            
            var model = auraModelFactory.Create();
            GeneralEditor.Value = model;

            model.AddTo(modelAnchors);
            tabName.SetDefaultValue(model.Name);

            model.Properties = Properties;
            
            model.WhenAnyValue(x => x.Name)
                .Subscribe(x => tabName.SetValue(x))
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
            Log.Debug($"Changed name of tab {tabName.DefaultValue}, {previousValue} => {tabName.Value}");
        }

        public override string ToString()
        {
            return new {TabName, DefaultTabName, IsSelected, IsFlipped}.DumpToTextRaw();
        }
    }
}
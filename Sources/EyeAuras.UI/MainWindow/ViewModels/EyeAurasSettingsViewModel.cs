using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Input;
using EyeAuras.UI.Prism.Modularity;
using JetBrains.Annotations;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI.Hotkeys;
using ReactiveUI;
using Unity;

namespace EyeAuras.UI.MainWindow.ViewModels
{
    internal sealed class EyeAurasSettingsViewModel : DisposableReactiveObject
    {
        private readonly IHotkeyConverter hotkeyConverter;
        private readonly IConfigProvider<EyeAurasConfig> configProvider;
        private HotkeyGesture freezeAurasHotkey;
        private HotkeyMode freezeAurasHotkeyMode;
        
        private HotkeyGesture unlockAurasHotkey;
        private HotkeyMode unlockAurasHotkeyMode;
        private bool isOpen;
        private HotkeyGesture selectRegionHotkey;

        public EyeAurasSettingsViewModel(
            [NotNull] IHotkeyConverter hotkeyConverter,
            [NotNull] IConfigProvider<EyeAurasConfig> configProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.hotkeyConverter = hotkeyConverter;
            this.configProvider = configProvider;

            this.WhenAnyValue(x => x.IsOpen)
                .Where(x => x)
                .Subscribe(LoadConfig)
                .AddTo(Anchors);

            SaveConfigCommand = CommandWrapper.Create(
                () =>
                {
                    SaveConfig();
                    IsOpen = false;
                });

            CancelCommand = CommandWrapper.Create(() => IsOpen = false);
        }

        public HotkeyGesture FreezeAurasHotkey
        {
            get => freezeAurasHotkey;
            set => RaiseAndSetIfChanged(ref freezeAurasHotkey, value);
        }

        public HotkeyMode FreezeAurasHotkeyMode
        {
            get => freezeAurasHotkeyMode;
            set => RaiseAndSetIfChanged(ref freezeAurasHotkeyMode, value);
        }
        
        public HotkeyGesture UnlockAurasHotkey
        {
            get => unlockAurasHotkey;
            set => RaiseAndSetIfChanged(ref unlockAurasHotkey, value);
        }

        public HotkeyGesture SelectRegionHotkey
        {
            get => selectRegionHotkey;
            set => this.RaiseAndSetIfChanged(ref selectRegionHotkey, value);
        }

        public HotkeyMode UnlockAurasHotkeyMode
        {
            get => unlockAurasHotkeyMode;
            set => RaiseAndSetIfChanged(ref unlockAurasHotkeyMode, value);
        }

        public bool IsOpen
        {
            get => isOpen;
            set => RaiseAndSetIfChanged(ref isOpen, value);
        }

        public ICommand SaveConfigCommand { get; }

        public ICommand CancelCommand { get; }

        private void SaveConfig()
        {
            var updatedConfig = configProvider.ActualConfig.CloneJson();
            updatedConfig.FreezeAurasHotkey = FreezeAurasHotkey?.ToString();
            updatedConfig.FreezeAurasHotkeyMode = FreezeAurasHotkeyMode;
            updatedConfig.UnlockAurasHotkey = UnlockAurasHotkey?.ToString();
            updatedConfig.UnlockAurasHotkeyMode = UnlockAurasHotkeyMode;
            updatedConfig.RegionSelectHotkey = SelectRegionHotkey?.ToString();
            configProvider.Save(updatedConfig);
        }

        private void LoadConfig()
        {
            var config = configProvider.ActualConfig;
            FreezeAurasHotkey = hotkeyConverter.ConvertFromString(config.FreezeAurasHotkey);
            FreezeAurasHotkeyMode = config.FreezeAurasHotkeyMode;
            UnlockAurasHotkey = hotkeyConverter.ConvertFromString(config.UnlockAurasHotkey);
            UnlockAurasHotkeyMode = config.UnlockAurasHotkeyMode;
            SelectRegionHotkey = hotkeyConverter.ConvertFromString(config.RegionSelectHotkey);
        }
    }
}
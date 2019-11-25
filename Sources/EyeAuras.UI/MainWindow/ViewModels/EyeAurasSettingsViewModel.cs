using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
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
    [UsedImplicitly]
    internal sealed class EyeAurasSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<EyeAurasConfig>
    {
        private readonly IHotkeyConverter hotkeyConverter;
        private readonly IConfigProvider<EyeAurasConfig> configProvider;
        private HotkeyGesture freezeAurasHotkey;
        private HotkeyMode freezeAurasHotkeyMode;
        
        private HotkeyGesture unlockAurasHotkey;
        private HotkeyMode unlockAurasHotkeyMode;
        private HotkeyGesture selectRegionHotkey;

        public EyeAurasSettingsViewModel(
            [NotNull] IHotkeyConverter hotkeyConverter,
            [NotNull] IConfigProvider<EyeAurasConfig> configProvider)
        {
            this.hotkeyConverter = hotkeyConverter;
            this.configProvider = configProvider;
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

        public string ModuleName { get; } = "EyeAuras Main Settings";
        
        public Task Load(EyeAurasConfig config)
        {
            FreezeAurasHotkey = hotkeyConverter.ConvertFromString(config.FreezeAurasHotkey);
            FreezeAurasHotkeyMode = config.FreezeAurasHotkeyMode;
            UnlockAurasHotkey = hotkeyConverter.ConvertFromString(config.UnlockAurasHotkey);
            UnlockAurasHotkeyMode = config.UnlockAurasHotkeyMode;
            SelectRegionHotkey = hotkeyConverter.ConvertFromString(config.RegionSelectHotkey);
            return Task.CompletedTask;
        }

        public EyeAurasConfig Save()
        {
            var updatedConfig = configProvider.ActualConfig.CloneJson();
            updatedConfig.FreezeAurasHotkey = FreezeAurasHotkey?.ToString();
            updatedConfig.FreezeAurasHotkeyMode = FreezeAurasHotkeyMode;
            updatedConfig.UnlockAurasHotkey = UnlockAurasHotkey?.ToString();
            updatedConfig.UnlockAurasHotkeyMode = UnlockAurasHotkeyMode;
            updatedConfig.RegionSelectHotkey = SelectRegionHotkey?.ToString();
            return updatedConfig;
        }
    }
}
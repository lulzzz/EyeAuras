using System.Windows;
using System.Windows.Forms;
using EyeAuras.UI.Core.Models;
using JetBrains.Annotations;
using PoeShared.Modularity;
using PoeShared.UI.Hotkeys;

namespace EyeAuras.UI.Prism.Modularity
{
    internal sealed class EyeAurasConfig : IPoeEyeConfigVersioned
    {
        public OverlayAuraProperties[] Auras { [CanBeNull] get; [CanBeNull] set; } = new OverlayAuraProperties[0];

        public string FreezeAurasHotkey { get; set; } = Keys.None.ToString();

        public HotkeyMode FreezeAurasHotkeyMode { get; set; } = HotkeyMode.Click;
        
        public string UnlockAurasHotkey { get; set; } = Keys.None.ToString();

        public HotkeyMode UnlockAurasHotkeyMode { get; set; } = HotkeyMode.Click;
        
        public string RegionSelectHotkey { get; set; } = Keys.None.ToString();
        
        public Rect MainWindowBounds { get; set; }
        
        public double ListWidth { get; set; }

        public int Version { get; set; } = 1;
    }
}
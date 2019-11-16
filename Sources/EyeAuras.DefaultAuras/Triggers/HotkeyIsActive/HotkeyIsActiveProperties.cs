using System.Windows.Forms;
using EyeAuras.Shared;
using PoeShared.UI.Hotkeys;

namespace EyeAuras.DefaultAuras.Triggers.HotkeyIsActive
{
    public sealed class HotkeyIsActiveProperties : IAuraProperties
    {
        public string Hotkey { get; set; } = Keys.None.ToString();

        public HotkeyMode HotkeyMode { get; set; } = HotkeyMode.Click;

        public bool SuppressKey { get; set; } = true;
        
        public bool TriggerValue { get; set; } = true;

        public int Version { get; set; } = 1;
    }
}
using EyeAuras.Shared;
using EyeAuras.Shared.Services;

namespace EyeAuras.DefaultAuras.Triggers.WinActive
{
    internal sealed class WinActiveTriggerProperties : IAuraProperties
    {
        public WindowMatchParams WindowMatchParams { get; set; }

        public int Version { get; set; } = 2;
    }
}
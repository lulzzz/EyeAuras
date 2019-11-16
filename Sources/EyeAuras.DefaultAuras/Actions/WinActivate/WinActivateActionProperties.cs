using EyeAuras.Shared;
using EyeAuras.Shared.Services;

namespace EyeAuras.DefaultAuras.Actions.WinActivate
{
    internal sealed class WinActivateActionProperties : IAuraProperties
    {
        public WindowMatchParams WindowMatchParams { get; set; }
        
        public int Version { get; set; } = 1;
    }
}
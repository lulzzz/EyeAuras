using EyeAuras.Shared;

namespace EyeAuras.UI.Triggers.AuraIsActive
{
    internal sealed class AuraIsActiveTriggerProperties : IAuraProperties
    {
        public string AuraId { get; set; }
        
        public int Version { get; set; } = 1;
    }
}
using EyeAuras.Shared;

namespace EyeAuras.DefaultAuras.Triggers.Default
{
    public sealed class DefaultTriggerProperties : IAuraProperties
    {
        public bool TriggerValue { get; set; } = true;

        public int Version { get; set; } = 1;
    }
}
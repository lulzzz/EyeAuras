using EyeAuras.Shared;

namespace EyeAuras.DefaultAuras.Actions.PlaySound
{
    internal sealed class PlaySoundActionProperties : IAuraProperties
    {
        public string Notification { get; set; }

        public int Version { get; set; } = 1;
    }
}
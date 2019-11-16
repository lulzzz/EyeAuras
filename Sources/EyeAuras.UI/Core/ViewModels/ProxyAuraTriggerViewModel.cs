using EyeAuras.Shared;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class ProxyAuraTriggerViewModel : ProxyAuraViewModel, IAuraTrigger
    {
        private string triggerDescription = "Technical Proxy Trigger";
        public string TriggerName { get; } = "ProxyTrigger";

        public string TriggerDescription
        {
            get => triggerDescription;
            private set => RaiseAndSetIfChanged(ref triggerDescription, value);
        }

        public bool IsActive { get; } = false;

        protected override void LoadProperties(IAuraProperties source)
        {
            base.LoadProperties(source);
            TriggerDescription = $"Technical Proxy Trigger: {source?.GetType().Name ?? "not initialized yet"}";
        }
    }
}
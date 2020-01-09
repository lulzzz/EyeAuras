using EyeAuras.Shared;
using EyeAuras.UI.Core.Models;

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

            var typeDescription = (source is ProxyAuraProperties proxyProperties)
                ? $"{proxyProperties.ModuleName} is not loaded yet"
                : $"{source.GetType().Name} is not initialized yet";
            TriggerDescription = $"Technical Proxy Trigger: {typeDescription}";
        }
    }
}
using EyeAuras.Shared;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class ProxyAuraActionViewModel : ProxyAuraViewModel, IAuraAction
    {
        private string actionDescription = "Technical Proxy Action";
        public string ActionName { get; } = "ProxyAction";

        public string ActionDescription
        {
            get => actionDescription;
            private set => RaiseAndSetIfChanged(ref actionDescription, value);
        }

        public void Execute()
        {
        }

        protected override void LoadProperties(IAuraProperties source)
        {
            base.LoadProperties(source);
            ActionDescription = $"Technical Proxy Action: {source?.GetType().Name ?? "not initialized yet"}";
        }
    }
}
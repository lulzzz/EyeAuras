using EyeAuras.Shared;
using PoeShared.Modularity;

namespace EyeAuras.UI.Core.ViewModels
{
    internal class ProxyAuraViewModel : AuraModelBase
    {
        private IAuraProperties proxyProperties;

        public IAuraProperties ProxyProperties
        {
            get => proxyProperties;
            set => RaiseAndSetIfChanged(ref proxyProperties, value);
        }

        protected override void LoadProperties(IAuraProperties source)
        {
            ProxyProperties = source;
        }

        protected override IAuraProperties SaveProperties()
        {
            return ProxyProperties;
        }
    }
}
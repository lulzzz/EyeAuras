using EyeAuras.Shared;
using EyeAuras.UI.Prism.Modularity;
using PoeShared.Modularity;

namespace EyeAuras.UI.Core.Models
{
    internal sealed class ProxyAuraProperties : IAuraProperties
    {
        public ProxyAuraProperties(PoeConfigMetadata<IAuraProperties> metadata)
        {
            this.Metadata = metadata;
        }

        public PoeConfigMetadata<IAuraProperties> Metadata { get; }

        public string ModuleName => Metadata.AssemblyName;

        public int Version
        {
            get => Metadata.Version ?? 0;
            set => Metadata.Version = value;
        } 
    }
}
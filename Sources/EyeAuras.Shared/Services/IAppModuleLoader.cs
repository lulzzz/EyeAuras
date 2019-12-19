using JetBrains.Annotations;
using Prism.Modularity;

namespace EyeAuras.Shared.Services
{
    public interface IAppModuleLoader
    {
        void LoadModulesFromBytes([NotNull] byte[] assemblyBytes);
    }
}
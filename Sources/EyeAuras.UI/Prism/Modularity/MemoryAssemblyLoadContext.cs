using System.Reflection;
using System.Runtime.Loader;
using log4net;

namespace EyeAuras.UI.Prism.Modularity
{
    internal sealed class MemoryAssemblyLoadContext : AssemblyLoadContext
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MemoryAssemblyLoadContext));
        
        public MemoryAssemblyLoadContext(string contextName) : base(contextName)
        {
            this.Resolving += OnResolving;
        }
        
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            Log.Debug($"[{Name}] Loading assembly {assemblyName}");
            var result = base.Load(assemblyName);
            if (result == null)
            {
                Log.Warn($"[{Name}] Failed to load assembly {assemblyName}");
            }
            else
            {
                Log.Debug($"[{Name}] Loaded assembly {assemblyName}: {result?.FullName}");
            }
            return result;
        }

        private Assembly? OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            Log.Debug($"[{Name}] Resolving assembly {assemblyName}");
            return null;
        }
    }
}
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
            return base.Load(assemblyName);
        }

        private Assembly? OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            Log.Debug($"[{Name}] Resolving assembly {assemblyName}");
            return null;
        }
    }
}
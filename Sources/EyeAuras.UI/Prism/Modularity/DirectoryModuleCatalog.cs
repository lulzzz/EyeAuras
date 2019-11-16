using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CommonServiceLocator;
using dnlib.DotNet;
using log4net;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using Prism.Modularity;
using IModule = Prism.Modularity.IModule;

namespace EyeAuras.UI.Prism.Modularity
{
    internal sealed class DirectoryModuleCatalog : ModuleCatalog
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DirectoryModuleCatalog));

        public DirectoryModuleCatalog(string modulesPath)
        {
            ModulesDirectory = new DirectoryInfo(modulesPath);
        }

        /// <summary>
        ///     Directory containing modules to search for.
        /// </summary>
        public DirectoryInfo ModulesDirectory { get; }

        /// <summary>
        ///     Drives the main logic of building the child domain and searching for the assemblies.
        /// </summary>
        protected override void InnerLoad()
        {
            LoadModuleCatalog();
        }

        private void LoadModuleCatalog()
        {
            if (!ModulesDirectory.Exists)
            {
                throw new InvalidOperationException($"Directory {ModulesDirectory} could not be found.");
            }

            var loadedAssemblies = (
                from Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()
                where !(assembly is AssemblyBuilder) &&
                      assembly.GetType().FullName != "System.Reflection.Emit.InternalAssemblyBuilder" &&
                      !string.IsNullOrEmpty(assembly.Location)
                select assembly).ToArray();

            Log.Debug($"Loaded assembly list:\n\t{loadedAssemblies.Select(x => new {x.FullName, x.Location}).DumpToTable()}");

            var moduleCatalog = ServiceLocator.Current.GetInstance<IModuleCatalog>();
            var manager = ServiceLocator.Current.GetInstance<IModuleManager>();
            var existingModules = moduleCatalog.Modules.ToArray();
            Log.Debug(
                $"Existing Modules list:\n\t{existingModules.Select(x => new {x.ModuleName, x.ModuleType, x.Ref, x.State, x.InitializationMode, x.DependsOn}).DumpToTable()}");

            var prismModuleInterfaceName = typeof(IDynamicModule).FullName;

            var potentialModules = (
                from dllFile in ModulesDirectory.GetFiles("*.dll")
                let loadedModules = loadedAssemblies.Where(x => !string.IsNullOrEmpty(x.Location))
                    .Select(x => new FileInfo(x.Location))
                    .Where(x => x.Exists)
                    .ToArray()
                let moduleContext = ModuleDef.CreateModuleContext()
                let module = ModuleDefMD.Load(dllFile.FullName, moduleContext)
                where !loadedModules.Contains(dllFile)
                select new {module, dllFile}).ToArray();

            var discoveredModules = (
                from item in potentialModules
                let types = item.module.GetTypes().Where(x => x.HasInterfaces).ToArray()
                let prismTypes = types.Where(x => x.IsClass && !x.IsAbstract)
                    .Where(x => x.Interfaces.Any(y => y.Interface.FullName == prismModuleInterfaceName))
                    .ToArray()
                where prismTypes.Any()
                from prismBootstrapper in prismTypes
                select new {item.dllFile, item.module, prismBootstrapper}).ToArray();

            Log.Debug(
                $"Discovered modules:\n\t{discoveredModules.Select(x => new {x.dllFile.FullName, x.module.Metadata.VersionString, x.prismBootstrapper.AssemblyQualifiedName}).DumpToTable()}");

            var modules = (
                from prismModule in discoveredModules
                let dependencies = new Collection<string>(existingModules.Select(x => x.ModuleName).ToArray())
                let moduleInfo = new ModuleInfo
                {
                    Ref = new Uri(prismModule.dllFile.FullName).AbsoluteUri,
                    InitializationMode = InitializationMode.OnDemand,
                    ModuleName = prismModule.prismBootstrapper.FullName,
                    ModuleType = prismModule.prismBootstrapper.AssemblyQualifiedName,
                    DependsOn = dependencies
                }
                where !existingModules.Any(x => x.Ref == moduleInfo.Ref || x.ModuleType == moduleInfo.ModuleType)
                select moduleInfo).ToArray();

            Log.Debug($"Will load following modules:\n\t{modules.Select(x => new {x.ModuleName, x.ModuleType, x.Ref}).DumpToTable()}");

            modules.ForEach(x => moduleCatalog.AddModule(x));
            foreach (var module in modules)
            {
                Log.Debug($"Loading module {new {module.ModuleName, module.ModuleType, module.Ref}}...");
                manager.LoadModule(module.ModuleName);
            }
        }
    }
}
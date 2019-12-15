using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.Windows;
using CommonServiceLocator;
using dnlib.DotNet;
using EyeAuras.Shared.Services;
using JetBrains.Annotations;
using log4net;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using Prism.Modularity;
using IModule = Prism.Modularity.IModule;

namespace EyeAuras.UI.Prism.Modularity
{
    internal sealed class SharedModuleCatalog : ModuleCatalog, IAppModuleLoader
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SharedModuleCatalog));
        private static readonly string PrismModuleInterfaceName = typeof(IDynamicModule).FullName;
        
        private IModuleCatalog moduleCatalog;
        private IModuleManager manager;
        private Collection<string> defaultModuleList;
        private readonly CompositeDisposable anchors = new CompositeDisposable();

        public SharedModuleCatalog()
        {
            var modulesDir = AppDomain.CurrentDomain.BaseDirectory;
            Log.Debug($"Creating {nameof(SharedModuleCatalog)}, modulesDirectory: {modulesDir}");
            ModulesDirectory = new DirectoryInfo(modulesDir);
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
            Log.Debug($"Initializing {nameof(SharedModuleCatalog)}, service locator: {ServiceLocator.Current} (isSet: {ServiceLocator.IsLocationProviderSet})");
            moduleCatalog = ServiceLocator.Current.GetInstance<IModuleCatalog>();
            manager = ServiceLocator.Current.GetInstance<IModuleManager>();
            defaultModuleList = new Collection<string>(moduleCatalog.Modules.Select(x => x.ModuleName).ToArray());
            Log.Debug($"Default modules list:\r\n\t {defaultModuleList.DumpToTable()}");
            LoadModuleCatalog();
        }

        private void LoadModuleCatalog()
        {
            if (!ModulesDirectory.Exists)
            {
                throw new InvalidOperationException($"Directory {ModulesDirectory} could not be found.");
            }

            var loadedAssemblies = GetLoadedAssemblies();
            Log.Debug($"Loaded assembly list:\n\t{loadedAssemblies.Select(x => new {x.FullName, x.Location}).DumpToTable()}");

            var existingModules = moduleCatalog.Modules.ToArray();
            Log.Debug(
                $"Default Modules list:\n\t{existingModules.Select(x => new {x.ModuleName, x.ModuleType, x.Ref, x.State, x.InitializationMode, x.DependsOn}).DumpToTable()}");

            var potentialModules = (
                from dllFile in ModulesDirectory.GetFiles("*.dll")
                let loadedModules = loadedAssemblies.Where(x => !string.IsNullOrEmpty(x.Location))
                    .Select(x => new FileInfo(x.Location))
                    .Where(x => x.Exists)
                    .ToArray()
                where !loadedModules.Contains(dllFile)
                let moduleContext = ModuleDef.CreateModuleContext()
                let dllFileData = File.ReadAllBytes(dllFile.FullName)
                let module = LoadModuleSafe(dllFileData, moduleContext, dllFile.FullName)
                where module != null
                select new {module, dllFile}).ToArray();

            var discoveredModules = (
                from item in potentialModules
                from prismBootstrapper in GetPrismBootstrapperTypes(item.module)
                select new {item.dllFile, item.module, prismBootstrapper}).ToArray();

            Log.Debug(
                $"Discovered {discoveredModules.Length} modules:\n\t{discoveredModules.Select(x => new {x.dllFile.FullName, x.module.Metadata.VersionString, x.prismBootstrapper.AssemblyQualifiedName}).DumpToTable()}");
            
            foreach (var module in discoveredModules)
            {
                Log.Debug($"Loading modules from file {module.dllFile}");
                var assemblyBytes = File.ReadAllBytes(module.dllFile.FullName);
                LoadAssembly(assemblyBytes);
                LoadModulesFromBytes(assemblyBytes);
            }
        }
        
        private ModuleInfo PrepareModuleInfo(IType prismBootstrapperType)
        {
            var result = new ModuleInfo
            {
                InitializationMode = InitializationMode.OnDemand,
                ModuleName = $"[Memory] {prismBootstrapperType.FullName}",
                ModuleType = prismBootstrapperType.AssemblyQualifiedName,
                DependsOn = defaultModuleList
            };

            return result;
        }

        private void LoadModule(ModuleInfo module)
        {
            Log.Debug($"Loading module {new {module.ModuleName, module.ModuleType, module.Ref}}");

            if (moduleCatalog == null)
            {
                throw new InvalidOperationException("Module catalog is not set yet");
            }
            
            var loadedModule = moduleCatalog.Modules.FirstOrDefault(x => x.ModuleName == module.ModuleName || x.ModuleType == module.ModuleType);
            if (loadedModule != null)
            {
                throw new ApplicationException($"Duplicate module found, loaded module: {loadedModule.DumpToTextRaw()}, module that was attempted to load: {module.DumpToTextRaw()}");
            }
            
            moduleCatalog.AddModule(module);
            manager.LoadModule(module.ModuleName);
        }
        
        private TypeDef[] GetPrismBootstrapperTypes(ModuleDefMD module)
        {
            var types = module.GetTypes().Where(x => x.HasInterfaces).ToArray();
            var prismTypes = types.Where(x => x.IsClass && !x.IsAbstract)
                .Where(x => x.Interfaces.Any(y => y.Interface.FullName == PrismModuleInterfaceName))
                .ToArray();
            return prismTypes;
        }

        private static Assembly[] GetLoadedAssemblies()
        {
            var loadedAssemblies = (
                from Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()
                where !(assembly is AssemblyBuilder) &&
                      assembly.GetType().FullName != "System.Reflection.Emit.InternalAssemblyBuilder" &&
                      !string.IsNullOrEmpty(assembly.Location)
                select assembly).ToArray();
            return loadedAssemblies;
        }
        
        private static ModuleDefMD LoadModuleSafe(byte[] assemblyBytes, ModuleContext moduleContext, string fileName = null)
        {
            try
            {
                return ModuleDefMD.Load(assemblyBytes, moduleContext);
            }
            catch (BadImageFormatException e)
            {
                Log.Warn($"Invalid .NET DLL format, binary{(string.IsNullOrEmpty(fileName) ? "from memory" : "from file " + fileName)} size: {assemblyBytes.Length} - {e.Message}");
                return null;
            }
            catch (Exception e)
            {
                Log.Warn($"Exception occured when tried to parse DLL metadata, binary size: {assemblyBytes.Length}", e);
                return null;
            }
        }

        private Assembly LoadAssembly(byte[] assemblyBytes)
        {
            Log.Debug($"Loading module from memory, binary data size: {assemblyBytes.Length}b...");
            
            var loadedAssemblies = GetLoadedAssemblies();
            Log.Debug($"Loaded assembly list:\n\t{loadedAssemblies.Select(x => new {x.FullName, x.Location}).DumpToTable()}");
             
            using var assemblyStream = new MemoryStream(assemblyBytes);
            var context = new AssemblyLoadContext(Guid.NewGuid().ToString());
            var assembly = context.LoadFromStream(assemblyStream);
            var assemblyName = assembly.GetName().Name;
            Log.Debug($"Successfully loaded .NET assembly from memory(name: {assemblyName}, size: {assemblyBytes.Length}): {new { assembly.FullName, assembly.EntryPoint, assembly.ImageRuntimeVersion, assembly.IsFullyTrusted  }}");
            
            return assembly;
        }
        
        private Assembly LoadAssembly(FileInfo dllFile)
        {
            Log.Debug($"Loading module from file, binary data size: {dllFile.Length}b...");
            var assembly = Assembly.LoadFrom(dllFile.FullName);
            var assemblyName = assembly.GetName().Name;
            Log.Debug($"Successfully loaded .NET assembly from file(name: {assemblyName}): {new { assembly.FullName, assembly.EntryPoint, assembly.ImageRuntimeVersion, assembly.IsFullyTrusted  }}");
            
            return assembly;
        }
        
        public void LoadModulesFromBytes(byte[] assemblyBytes)
        {
            Log.Debug($"Trying to load Prism module definition from byte array, size: {assemblyBytes.Length}");
            var moduleContext = new ModuleContext();
            var moduleDef = LoadModuleSafe(assemblyBytes, moduleContext);
            var prismBootstrappers = GetPrismBootstrapperTypes(moduleDef);
            if (!prismBootstrappers.Any())
            {
                throw new InvalidOperationException($"Failed to find any Prism-compatible type implementing {PrismModuleInterfaceName} in assembly {moduleDef.FullName}");
            }

            //var assembly = LoadAssembly(assemblyBytes);
            foreach (var bootstrapperType in prismBootstrappers)
            {
                Log.Debug($"Loading type {bootstrapperType}");
                var moduleInfo = PrepareModuleInfo(bootstrapperType);
                LoadModule(moduleInfo);
            }
        }    
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using EyeAuras.DefaultAuras.Prism;
using EyeAuras.Shared.Services;
using EyeAuras.UI.MainWindow.ViewModels;
using EyeAuras.UI.Prism.Modularity;
using EyeAuras.UI.SplashWindow.Services;
using log4net;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Squirrel.Prism;
using Prism.Modularity;
using Prism.Unity;
using Unity;
using Unity.Lifetime;

namespace EyeAuras.UI.Prism
{
    internal sealed class EyeAurasBootstrapper : UnityBootstrapper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EyeAurasBootstrapper));

        private readonly CompositeDisposable anchors = new CompositeDisposable();

        public EyeAurasBootstrapper()
        {
            Log.Debug($"Initializing EyeAuras bootstrapper");
        }

        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow.Views.MainWindow>();
        }

        protected override void InitializeShell()
        {
            base.InitializeShell();

            Log.Info("Initializing shell...");
            var sw = Stopwatch.StartNew();

            var window = (Window) Shell;

            var splashWindow = new SplashWindowDisplayer(window);

            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => window.ContentRendered += h, h => window.ContentRendered -= h)
                .Take(1)
                .Subscribe(
                    () =>
                    {
                        Log.Debug("Window rendered");
                        Application.Current.MainWindow = window;
                        splashWindow.Close();
                        Log.Info($"Window+Shell initialization has taken {sw.ElapsedMilliseconds}ms");
                    })
                .AddTo(anchors);

            Observable
                .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => window.Loaded += h, h => window.Loaded -= h)
                .Take(1)
                .Subscribe(
                    () =>
                    {
                        Log.Debug("Window loaded");
                        Log.Info($"Shell initialization has taken {sw.ElapsedMilliseconds}ms");
                    })
                .AddTo(anchors);

            Log.Info("Loading main window...");
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            Application.Current.MainWindow = window;

            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            splashWindow.Show();
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new SharedModuleCatalog();
        }

        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();
            Log.Debug($"Configuring {Container}, catalog: {ModuleCatalog} (type: {ModuleCatalog.GetType()})");
            if (ModuleCatalog is IAppModuleLoader appModuleLoader)
            {
                Container.RegisterInstance(appModuleLoader, new ContainerControlledLifetimeManager());
            }
            else
            {
                throw new ApplicationException($"ModuleCatalog must be of type {nameof(IAppModuleLoader)}, got {ModuleCatalog}");
            }
        }

        protected override void ConfigureModuleCatalog()
        {
            var poeSharedModule = typeof(PoeSharedModule);
            ModuleCatalog.AddModule(
                new ModuleInfo
                {
                    ModuleName = poeSharedModule.Name,
                    ModuleType = poeSharedModule.AssemblyQualifiedName
                });
            
            var poeSharedWpfModule = typeof(PoeSharedWpfModule);
            ModuleCatalog.AddModule(
                new ModuleInfo
                {
                    ModuleName = poeSharedWpfModule.Name,
                    ModuleType = poeSharedWpfModule.AssemblyQualifiedName,
                    DependsOn = new[] { poeSharedModule.Name }.ToObservableCollection(),
                });

            var mainModule = typeof(MainModule);
            ModuleCatalog.AddModule(
                new ModuleInfo
                {
                    ModuleName = mainModule.Name,
                    ModuleType = mainModule.AssemblyQualifiedName,
                    DependsOn = new[] { poeSharedWpfModule.Name }.ToObservableCollection(),
                });
            
            var updaterModule = typeof(UpdaterModule);
            ModuleCatalog.AddModule(
                new ModuleInfo
                {
                    ModuleName = updaterModule.Name,
                    ModuleType = updaterModule.AssemblyQualifiedName,
                    DependsOn = new[] { mainModule.Name, poeSharedModule.Name, poeSharedWpfModule.Name }.ToObservableCollection(),
                });
        }

        public override void Run(bool runWithDefaultConfiguration)
        {
            base.Run(runWithDefaultConfiguration);

            var moduleManager = Container.Resolve<IModuleManager>();
            Observable
                .FromEventPattern<LoadModuleCompletedEventArgs>(h => moduleManager.LoadModuleCompleted += h, h => moduleManager.LoadModuleCompleted -= h)
                .Select(x => x.EventArgs)
                .Subscribe(
                    evt =>
                    {
                        if (evt.Error != null)
                        {
                            Log.Error($"[#{evt.ModuleInfo.ModuleName}] Error during loading occured, isHandled: {evt.IsErrorHandled}", evt.Error);
                        }

                        Log.Info($"[#{evt.ModuleInfo.ModuleName}] Module loaded");
                    })
                .AddTo(anchors);

            var moduleCatalog = Container.Resolve<IModuleCatalog>();
            var modules = moduleCatalog.Modules.ToArray();
            Log.Debug(
                $"Modules list:\n\t{modules.Select(x => new {x.ModuleName, x.ModuleType, x.State, x.InitializationMode, x.DependsOn}).DumpToTable()}");

            var window = (Window) Shell;
            
            var sw = Stopwatch.StartNew();
            Log.Info("Initializing Main window...");
            var viewModel = Container.Resolve<IMainWindowViewModel>();
            window.DataContext = viewModel.AddTo(anchors);
            sw.Stop();
            Log.Info($"Main window initialization took {sw.ElapsedMilliseconds}ms...");
        }

        public void Dispose()
        {
            Log.Info("Disposing Main bootstrapper...");
            anchors.Dispose();
        }
    }
}
using System.Collections.ObjectModel;
using DynamicData;
using PoeShared.Scaffolding;
using Prism.Modularity;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData.Binding;
using log4net;
using PoeShared;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;

namespace EyeAuras.UI.MainWindow.ViewModels
{
    internal sealed class PrismModuleStatusViewModel : DisposableReactiveObject, IPrismModuleStatusViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PrismModuleStatusViewModel));

        private readonly IModuleManager moduleManager;
        private readonly ISourceList<PrismModuleStatus> moduleList = new SourceList<PrismModuleStatus>();
        private readonly ObservableAsPropertyHelper<bool> allModulesLoaded;
        
        public PrismModuleStatusViewModel(
            IModuleCatalog moduleCatalog,
            IModuleManager moduleManager)
        {
            IsVisible = AppArguments.Instance.IsDebugMode;
            
            this.moduleManager = moduleManager;
            moduleList
                .Connect()
                .Bind(out var modules)
                .Subscribe()
                .AddTo(Anchors);

            Modules = modules;

            Observable.FromEventPattern<EventHandler<LoadModuleCompletedEventArgs>, LoadModuleCompletedEventArgs>(
                    h => moduleManager.LoadModuleCompleted += h,
                    h => moduleManager.LoadModuleCompleted -= h)
                .StartWithDefault()
                .Select(() => moduleCatalog.Modules.Select(x => new PrismModuleStatus(x)).ToArray())
                .DistinctUntilChanged()
                .Subscribe(
                    items =>
                    {
                        moduleList.Clear();
                        moduleList.AddRange(items);
                    })
                .AddTo(Anchors);

            allModulesLoaded = modules.ToObservableChangeSet()
                .Select(() => modules.Any() && modules.All(x => x.IsLoaded))
                .ToPropertyHelper(this, x => x.AllModulesLoaded)
                .AddTo(Anchors);
            
            LoadModuleCommand = CommandWrapper.Create<PrismModuleStatus>(LoadModuleCommandExecuted, LoadModuleCommandCanExecute);
        }

        private bool LoadModuleCommandCanExecute(PrismModuleStatus arg)
        {
            return arg != null && !arg.IsLoaded;
        }

        private void LoadModuleCommandExecuted(PrismModuleStatus arg)
        {
            Log.Debug($"Loading module {new {arg.ModuleName, arg.IsLoaded}}");
            moduleManager.LoadModule(arg.ModuleName);
            Log.Debug($"Module loaded: {new {arg.ModuleName, arg.IsLoaded}}");
        }

        public ReadOnlyObservableCollection<PrismModuleStatus> Modules { get; }

        public bool AllModulesLoaded => allModulesLoaded.Value;
        
        public ICommand LoadModuleCommand { get; }
        
        public bool IsVisible { get; }
    }
}
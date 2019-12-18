using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using DynamicData;
using EyeAuras.Shared.Services;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.MainWindow.Models
{
    internal sealed class MainWindowBlocksService : DisposableReactiveObject, IMainWindowBlocksProvider, IMainWindowBlocksRepository
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MainWindowBlocksService));

        private readonly ISourceList<object> statusBarItemsSource = new SourceList<object>();
        
        public MainWindowBlocksService()
        {
            statusBarItemsSource
                .Connect()
                .Bind(out var statusBarItems)
                .Subscribe()
                .AddTo(Anchors);

            StatusBarItems = statusBarItems;
        }

        public ReadOnlyObservableCollection<object> StatusBarItems { get; }
        
        public IDisposable AddStatusBarItem(object item)
        {
            Guard.ArgumentNotNull(item, nameof(item));
            
            Log.Debug($"Adding item {item} to StatusBar, items: {StatusBarItems.DumpToTextRaw()}");
            statusBarItemsSource.Add(item);

            return Disposable.Create(
                () =>
                {
                    Log.Debug($"Removing item {item} from StatusBar, items: {StatusBarItems.DumpToTextRaw()}");
                    statusBarItemsSource.Remove(item);
                });
        }
    }
}
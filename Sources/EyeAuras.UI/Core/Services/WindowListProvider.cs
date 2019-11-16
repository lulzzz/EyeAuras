using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using EyeAuras.OnTopReplica;
using EyeAuras.OnTopReplica.WindowSeekers;
using EyeAuras.Shared.Services;
using log4net;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Core.Services
{
    internal sealed class WindowListProvider : DisposableReactiveObject, IWindowListProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowListProvider));
        private static readonly IEqualityComparer<WindowHandle> WindowComparer = new LambdaComparer<WindowHandle>((x, y) => x?.Handle == y?.Handle && string.Compare(x?.Title, y?.Title, StringComparison.Ordinal) == 0);

        private readonly ReadOnlyObservableCollection<WindowHandle> windowList;
        private readonly SourceList<WindowHandle> windowListSource;

        private readonly BaseWindowSeeker windowSeeker;
        

        public WindowListProvider()
        {
            windowListSource = new SourceList<WindowHandle>();
            windowListSource
                .Connect()
                .Filter(x => !string.IsNullOrWhiteSpace(x.Title))
                .Sort(new SortExpressionComparer<WindowHandle>().ThenByAscending(x => x.Title).ThenByAscending(x => x.Title?.Length ?? int.MaxValue))
                .ObserveOnDispatcher()
                .Bind(out windowList)
                .Subscribe()
                .AddTo(Anchors);

            windowSeeker = new TaskWindowSeeker
            {
                SkipNotVisibleWindows = true
            };

            Observable.Timer(DateTimeOffset.Now, TimeSpan.FromSeconds(1))
                .Subscribe(RefreshWindowList)
                .AddTo(Anchors);
        }

        public ReadOnlyObservableCollection<WindowHandle> WindowList => windowList;
        
        private void RefreshWindowList()
        {
            windowSeeker.Refresh();
            var existingWindows = windowListSource.Items.ToArray();
            var itemsToAdd = windowSeeker.Windows.Except(existingWindows, WindowComparer).ToArray();
            var itemsToRemove = existingWindows.Except(windowSeeker.Windows, WindowComparer).ToArray();
            windowListSource.RemoveMany(itemsToRemove);
            windowListSource.AddRange(itemsToAdd);
        }
    }
}
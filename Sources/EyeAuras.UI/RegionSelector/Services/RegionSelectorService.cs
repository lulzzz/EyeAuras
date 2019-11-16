using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using EyeAuras.OnTopReplica.WindowSeekers;
using EyeAuras.UI.RegionSelector.ViewModels;
using EyeAuras.UI.RegionSelector.Views;
using log4net;
using MaterialDesignThemes.Wpf;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.RegionSelector.Services
{
    internal sealed class RegionSelectorService : DisposableReactiveObject
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RegionSelectorService));

        private readonly IFactory<RegionSelectorWindow> regionSelectorWindowFactory;

        private readonly SerialDisposable activeWindowAnchors = new SerialDisposable();

        public RegionSelectorService(IFactory<RegionSelectorWindow> regionSelectorWindowFactory)
        {
            this.regionSelectorWindowFactory = regionSelectorWindowFactory;
            activeWindowAnchors.AddTo(Anchors);
        }

        public IObservable<RegionSelectorResult> Select()
        {
            var anchors = new CompositeDisposable().AssignTo(activeWindowAnchors);

            var window = regionSelectorWindowFactory.Create().AddTo(anchors);

            Log.Debug($"Created new selector window: {window}");

            var result = Observable.FromEventPattern<EventHandler, EventArgs>(h => window.Closed += h, h => window.Closed -= h)
                .Select(x => window.Result)
                .Take(1);
            
            window.Show();

            return result;
        }
    }
}
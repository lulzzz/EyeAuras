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
using EyeAuras.DefaultAuras.Triggers.Default;
using EyeAuras.OnTopReplica.WindowSeekers;
using EyeAuras.UI.Core.Services;
using EyeAuras.UI.MainWindow.Models;
using EyeAuras.UI.RegionSelector.ViewModels;
using EyeAuras.UI.RegionSelector.Views;
using log4net;
using MaterialDesignThemes.Wpf;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.RegionSelector.Services
{
    internal sealed class RegionSelectorService : DisposableReactiveObject, IRegionSelectorService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RegionSelectorService));

        private readonly ISharedContext sharedContext;
        private readonly IFactory<RegionSelectorWindow> regionSelectorWindowFactory;

        public RegionSelectorService(
            ISharedContext sharedContext,
            IFactory<RegionSelectorWindow> regionSelectorWindowFactory)
        {
            this.sharedContext = sharedContext;
            this.regionSelectorWindowFactory = regionSelectorWindowFactory;
        }

        public IObservable<RegionSelectorResult> SelectRegion()
        {
            return Observable.Create<RegionSelectorResult>(
                observer =>
                {
                    var windowAnchors = new CompositeDisposable();

                    var temporarilyDisableAuras = new DefaultTrigger() {IsActive = false};
                    Disposable.Create(() => sharedContext.SystemTrigger.Triggers.Remove(temporarilyDisableAuras)).AddTo(windowAnchors);
                    temporarilyDisableAuras.AddTo(sharedContext.SystemTrigger.Triggers);

                    var window = regionSelectorWindowFactory.Create().AddTo(windowAnchors);
                    Disposable.Create(() => Log.Debug("Disposed selector window: {window}")).AddTo(windowAnchors);
                    Log.Debug($"Created new selector window: {window}");

                    Observable.FromEventPattern<EventHandler, EventArgs>(h => window.Closed += h, h => window.Closed -= h)
                        .Select(x => window.Result)
                        .Take(1)
                        .Subscribe(observer)
                        .AddTo(windowAnchors);
            
                    window.Show();
            
                    return windowAnchors;
                });
        }
    }
}
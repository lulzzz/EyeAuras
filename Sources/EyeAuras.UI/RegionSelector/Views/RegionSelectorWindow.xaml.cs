using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using EyeAuras.OnTopReplica;
using EyeAuras.Shared.Services;
using EyeAuras.UI.MainWindow;
using EyeAuras.UI.RegionSelector.Services;
using EyeAuras.UI.RegionSelector.ViewModels;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.UI;
using ReactiveUI;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace EyeAuras.UI.RegionSelector.Views
{
    public partial class RegionSelectorWindow : IDisposable
    {
        private readonly IRegionSelectorViewModel viewModel;
        private static readonly ILog Log = LogManager.GetLogger(typeof(RegionSelectorWindow));

        private readonly CompositeDisposable anchors = new CompositeDisposable();
        private readonly SelectionAdorner selectionAdorner;

        public RegionSelectorWindow(
            [NotNull] IFactory<IRegionSelectorViewModel, ICloseController<RegionSelectorResult>> viewModelFactory)
        {
            InitializeComponent();
            selectionAdorner = new SelectionAdorner(RegionSelectorRoot) {Stroke = Brushes.Lime};

            Disposable.Create(() => Log.Debug("RegionSelectorWindow disposed")).AddTo(anchors);
            Closed += OnClosed;
            Loaded += OnLoaded;

            CloseController = new ParameterizedCloseController<Window, RegionSelectorResult>(this,
                result =>
                {
                    var windowHandle = new WindowInteropHelper(this).Handle;
                    var state = new {IsVisible, Visibility, IsActive};
                    if (state.IsVisible && state.Visibility == Visibility.Visible)
                    {
                        Log.Debug($"[{windowHandle.ToHexadecimal()}] Closing RegionSelector window({state}), result - {result.DumpToTextRaw()}");
                        Result = result;
                        Close();
                    }
                    else
                    {
                        Log.Debug($"[{windowHandle.ToHexadecimal()}] Ignoring Close request for RegionSelector window({state}) - already closed");
                    }
                });
            
            viewModel = viewModelFactory.Create(CloseController).AddTo(anchors);
            DataContext = viewModel;
        }

        public RegionSelectorResult Result { get; private set; }
        
        public ICloseController<RegionSelectorResult> CloseController { get; }

        public void Dispose()
        {
            anchors?.Dispose();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var windowHandle = new WindowInteropHelper(this).Handle;
            Log.Debug($"[{windowHandle.ToHexadecimal()}] Window loaded");

            if (!UnsafeNative.SetForegroundWindow(windowHandle))
            {
                Log.Warn($"[{windowHandle.ToHexadecimal()}] Failed to bring window to front");
            }
            var closeWindowSink = new Subject<string>();

            Disposable.Create(() => closeWindowSink.OnNext("window is disposed"))
                .AddTo(anchors);

            Observable.Merge(
                    Observable.FromEventPattern<KeyEventHandler, KeyEventArgs>(h => PreviewKeyDown += h, h => PreviewKeyDown -= h)
                        .Where(x => x.EventArgs.Key == Key.Escape)
                        .Select(x => "ESC pressed"),
                    Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => LostFocus += h, h => LostFocus -= h).Select(x => "window LostFocus"),
                    Observable.FromEventPattern<EventHandler, EventArgs>(h => Deactivated += h, h => Deactivated -= h).Select(x => "window Deactivated"))
                .Subscribe(closeWindowSink)
                .AddTo(anchors);

            closeWindowSink
                .Take(1)
                .Subscribe(reason => CloseController.Close(new RegionSelectorResult() { Reason = reason }))
                .AddTo(anchors);

            var adornerLayer = AdornerLayer.GetAdornerLayer(RegionSelectorRoot);
            Guard.ArgumentNotNull(adornerLayer, nameof(adornerLayer));
            adornerLayer.Add(selectionAdorner);
            
            selectionAdorner.StartSelection()
                .Subscribe(
                    region =>
                    {
                        var screenRegion = GeometryExtensions.ToScreen(region, selectionAdorner);
                        viewModel.Selection = screenRegion;
                    },
                    Log.HandleException,
                    () => { closeWindowSink.OnNext("region selection cancelled"); })
                .AddTo(anchors);

            viewModel.WhenAnyValue(x => x.MouseRegion)
                .Subscribe(
                    regionResult =>
                    {
                        if (regionResult == null || !regionResult.IsValid)
                        {
                            RegionCandidate.Visibility = Visibility.Collapsed;
                            return;
                        } 
                        
                        RegionCandidate.Visibility = Visibility.Visible;
                        
                        var bounds = GeometryExtensions.FromScreen(regionResult.Window.WindowBounds);
                        Canvas.SetLeft(RegionCandidate, bounds.X);
                        Canvas.SetTop(RegionCandidate, bounds.Y);
                        RegionCandidate.Width = bounds.Width;
                        RegionCandidate.Height = bounds.Height;
                    })
                .AddTo(anchors);
        }

        private void OnClosed(object sender, EventArgs e)
        {
            Dispose();
        }
    }
}
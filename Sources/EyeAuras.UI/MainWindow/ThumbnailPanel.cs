using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WindowsFormsAero.Dwm;
using EyeAuras.OnTopReplica;
using log4net;
using PoeShared;
using PoeShared.Scaffolding;
using ReactiveUI;
using WinSize = System.Drawing.Size;
using WinPoint = System.Drawing.Point;
using WinRectangle = System.Drawing.Rectangle;

namespace EyeAuras.UI.MainWindow
{
    public class ThumbnailPanel : Panel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ThumbnailPanel));
        
        private static readonly TimeSpan UpdateLogSamplingInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(1);

        public static readonly DependencyProperty TargetWindowProperty = DependencyProperty.Register(
            "TargetWindow",
            typeof(WindowHandle),
            typeof(ThumbnailPanel),
            new FrameworkPropertyMetadata(default(WindowHandle)));

        public static readonly DependencyProperty RegionProperty = DependencyProperty.Register(
            "Region",
            typeof(ThumbnailRegion),
            typeof(ThumbnailPanel),
            new FrameworkPropertyMetadata(default(ThumbnailRegion)));

        public static readonly DependencyProperty OwnerProperty = DependencyProperty.Register(
            "Owner",
            typeof(Window),
            typeof(ThumbnailPanel),
            new PropertyMetadata(default(Window)));

        public static readonly DependencyProperty ThumbnailSizeProperty = DependencyProperty.Register(
            "ThumbnailSize",
            typeof(WinSize),
            typeof(ThumbnailPanel),
            new PropertyMetadata(default(WinSize)));

        public static readonly DependencyProperty ThumbnailProperty = DependencyProperty.Register(
            "Thumbnail",
            typeof(Thumbnail),
            typeof(ThumbnailPanel),
            new PropertyMetadata(default(Thumbnail)));

        public static readonly DependencyProperty LocationProperty = DependencyProperty.Register(
            "Location",
            typeof(WinPoint),
            typeof(ThumbnailPanel),
            new PropertyMetadata(default(WinPoint)));

        public static readonly DependencyProperty ThumbnailOpacityProperty = DependencyProperty.Register(
            "ThumbnailOpacity",
            typeof(double),
            typeof(ThumbnailPanel),
            new PropertyMetadata((double) 1));

        public static readonly DependencyProperty TargetWindowSizeProperty = DependencyProperty.Register(
            "TargetWindowSize",
            typeof(WinSize),
            typeof(ThumbnailPanel),
            new PropertyMetadata(default(WinSize)));

        public static readonly DependencyProperty IsInSelectModeProperty = DependencyProperty.Register(
            "IsInSelectMode",
            typeof(bool),
            typeof(ThumbnailPanel),
            new PropertyMetadata(default(bool)));

        private readonly CompositeDisposable anchors = new CompositeDisposable();
        private readonly ISubject<Size> renderSizeSource = new Subject<Size>();
        private readonly SelectionAdorner selectionAdorner;

        public ThumbnailPanel()
        {
            Dpi = VisualTreeHelper.GetDpi(this);
            Loaded += OnLoaded;
            
            selectionAdorner = new SelectionAdorner(this);
            selectionAdorner.Stroke = Brushes.Lime;
        }

        public bool IsInSelectMode
        {
            get => (bool) GetValue(IsInSelectModeProperty);
            set => SetValue(IsInSelectModeProperty, value);
        }

        public WinSize TargetWindowSize
        {
            get => (WinSize) GetValue(TargetWindowSizeProperty);
            set => SetValue(TargetWindowSizeProperty, value);
        }

        public double ThumbnailOpacity
        {
            get => (double) GetValue(ThumbnailOpacityProperty);
            set => SetValue(ThumbnailOpacityProperty, value);
        }

        public WinPoint Location
        {
            get => (WinPoint) GetValue(LocationProperty);
            set => SetValue(LocationProperty, value);
        }

        public Thumbnail Thumbnail
        {
            get => (Thumbnail) GetValue(ThumbnailProperty);
            private set => SetValue(ThumbnailProperty, value);
        }

        public WinSize ThumbnailSize
        {
            get => (WinSize) GetValue(ThumbnailSizeProperty);
            set => SetValue(ThumbnailSizeProperty, value);
        }

        public Window Owner
        {
            get => (Window) GetValue(OwnerProperty);
            private set => SetValue(OwnerProperty, value);
        }

        public ThumbnailRegion Region
        {
            get => (ThumbnailRegion) GetValue(RegionProperty);
            set => SetValue(RegionProperty, value);
        }

        public WindowHandle TargetWindow
        {
            get => (WindowHandle) GetValue(TargetWindowProperty);
            set => SetValue(TargetWindowProperty, value);
        }

        public DpiScale Dpi { get; }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Log.Debug($"ThumbnailPanel loaded, TargetWindow: {TargetWindow}, parent: {Parent}");
            if (Owner != null)
            {
                throw new InvalidOperationException("ThumbnailPanel must be initialized only once per lifecycle");
            }
            Owner = this.FindVisualAncestor<Window>();
            Guard.ArgumentNotNull(Owner, nameof(Owner));
            
            Observable.Merge(
                    this.Observe(OwnerProperty).ToUnit(),
                    this.Observe(IsVisibleProperty).ToUnit(),
                    this.Observe(TargetWindowProperty).ToUnit())
                .StartWithDefault()
                .Do(_ => UpdateThumbnailHandle())
                .RetryWithDelay(RetryInterval, DispatcherScheduler.Current)
                .Subscribe(args =>
                {
                    Log.Debug($"Updating Thumbnail Handle (target: {TargetWindow})");
                }, Log.HandleUiException)
                .AddTo(anchors);
            
            var updateArgsSource =
                Observable.Merge(
                        this.Observe(RegionProperty).Select(x => Region.WhenAnyValue(y => y.Bounds)).Switch().ToUnit(),
                        this.Observe(LocationProperty).ToUnit(),
                        this.Observe(ThumbnailProperty).ToUnit(),
                        this.Observe(ThumbnailOpacityProperty).ToUnit(),
                        renderSizeSource.ToUnit(),
                        this.Observe(ThumbnailSizeProperty).ToUnit())
                    .StartWithDefault()
                    .Where(x => CanUpdateThumbnail())
                    .Select(PrepareUpdateArgs)
                    .DistinctUntilChanged()
                    .RetryWithDelay(RetryInterval, DispatcherScheduler.Current)
                    .Publish();
            updateArgsSource.Connect().AddTo(anchors);
            
            updateArgsSource
                .Subscribe(UpdateThumbnail, Log.HandleUiException)
                .AddTo(anchors);
            
            updateArgsSource
                .Sample(UpdateLogSamplingInterval)
                .Subscribe(args =>
                {
                    Log.Debug($"Updating Thumbnail (target: {args.TargetWindow}), args: {args}");
                }, Log.HandleUiException)
                .AddTo(anchors);

            this.Observe(IsInSelectModeProperty)
                .Select(x => IsInSelectMode)
                .Select(
                    () =>
                    {
                        var currentCursor = Cursor;
                        Cursor = Cursors.Cross;
                        return IsInSelectMode
                            ? selectionAdorner.StartSelection()
                                .Finally(
                                    () =>
                                    {
                                        IsInSelectMode = false;
                                        Cursor = currentCursor;
                                    })
                            : Observable.Return(Rect.Empty);
                    })
                .Switch()
                .Where(GeometryExtensions.IsNotEmpty)
                .Where(selection => selection.Width * selection.Height >= 10)
                .Subscribe(UpdateRegion, Log.HandleUiException)
                .AddTo(anchors);

            var adornerLayer = AdornerLayer.GetAdornerLayer(this);
            Guard.ArgumentNotNull(adornerLayer, nameof(adornerLayer));
            adornerLayer.Add(selectionAdorner);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            renderSizeSource.OnNext(sizeInfo.NewSize);
        }

        private void UpdateThumbnailHandle()
        {
            try
            {
                if (Thumbnail != null && !Thumbnail.IsInvalid)
                {
                    Log.Debug($"Disposing current Thumbnail: {Thumbnail}");
                    Thumbnail.Close();
                    Thumbnail.Dispose();
                }
                Thumbnail = null;

                if (TargetWindow == null || Owner == null || !IsVisible)
                {
                    return;
                }

                Log.Debug(
                    $"Reconfiguring Thumbnail, targetWindow: {TargetWindow} region: {Region}");

                if (Owner == null)
                {
                    return;
                }

                var ownerFormHelper = new WindowInteropHelper(Owner);

                var thumbnail = DwmManager.Register(ownerFormHelper.Handle, TargetWindow.Handle);
                thumbnail.ShowOnlyClientArea = true;

                Thumbnail = thumbnail;
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to create Thumbnail Handle for window {TargetWindow}", e);
                throw;
            }
        }
        
        private ThumbnailArgs PrepareUpdateArgs()
        {
            try
            {
                Guard.ArgumentIsTrue(CanUpdateThumbnail(), "CanUpdateThumbnail");

                TargetWindowSize = Thumbnail.GetSourceSize();
                ThumbnailSize = Region == null || Region.Bounds.IsEmpty || Region.RegionWidth <= 0 || Region.RegionHeight <= 0 || !GeometryExtensions.IsNotEmpty(Region.Bounds)
                    ? TargetWindowSize
                    : Region.ComputeRegionSize(TargetWindowSize);

                var canvasSize = RenderSize;

                var ownerLocation = TranslatePoint(new Point(0, 0), Owner);
                Location = GeometryExtensions.ToWinPoint(ownerLocation);
                var destination = new Rect(
                    Math.Floor(Location.X * Dpi.DpiScaleX),
                    Math.Floor(Location.Y * Dpi.DpiScaleY),
                    Math.Ceiling(canvasSize.Width * Dpi.DpiScaleX),
                    Math.Ceiling(canvasSize.Height * Dpi.DpiScaleY));

                WinRectangle source;
                var regionBounds = Region?.Bounds ?? WinRectangle.Empty;
                if (regionBounds.IsEmpty)
                {
                    source = new WinRectangle(0, 0, ThumbnailSize.Width, ThumbnailSize.Height);
                }
                else
                {
                    source = new WinRectangle(
                        regionBounds.X,
                        regionBounds.Y,
                        regionBounds.Width > 0 && regionBounds.Height > 0
                            ? regionBounds.Width
                            : ThumbnailSize.Width,
                        regionBounds.Width > 0 && regionBounds.Height > 0
                            ? regionBounds.Height
                            : ThumbnailSize.Height);
                }
                    
                var args = new ThumbnailArgs
                {
                    Destination = GeometryExtensions.ToWinRectangle(destination),
                    Source = source,
                    Opacity = ToByte(ThumbnailOpacity),
                    TargetWindowSize = TargetWindowSize,
                    DestinationBounds = regionBounds,
                    TargetWindow = TargetWindow,
                    ThumbnailSize = ThumbnailSize,
                    Thumbnail = Thumbnail,
                    RenderSize = RenderSize
                };

                return args;
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to build ThumbnailArgs, state: { new { Region, Thumbnail, TargetWindow, ThumbnailSize, TargetWindowSize } }", e);
                throw;
            }
        }
        
        private bool CanUpdateThumbnail()
        {
            try
            {
                return Thumbnail != null && !Thumbnail.IsInvalid && Owner != null;
            }
            catch (Exception e)
            {
                Log.Warn("Failed to check CanUpdateThumbnail", e);
                throw;
            }
        }
        
        private static void UpdateThumbnail(ThumbnailArgs args)
        {
            try
            {
                args.Thumbnail.Update(
                    destination: args.Destination, 
                    source: args.Source, 
                    opacity: args.Opacity, 
                    visible: true,
                    onlyClientArea: true);
            }
            catch (Exception ex)
            {
                Log.Error("UpdateThumbnail error", ex);
                args.Thumbnail.Dispose();
            }
        }

        private void UpdateRegion(Rect selection)
        {
            if (selection.IsEmpty)
            {
                Region.SetValue(Region.Bounds);
                return;
            }
            
            selection.Scale(Dpi.DpiScaleX, Dpi.DpiScaleY); // Wpf Px => Win Px
            var targetSize = TargetWindowSize; // Win Px
            var destinationSize = RenderSize.Scale(Dpi.DpiScaleX, Dpi.DpiScaleY); // Win Px
            var currentTargetRegion = Region.Bounds; // Win Px

            var selectionPercent = new Rect
            {
                X = selection.X / destinationSize.Width,
                Y = selection.Y / destinationSize.Height,
                Height = selection.Height / destinationSize.Height,
                Width = selection.Width / destinationSize.Width
            };

            Rect currentRegionPercent;
            if (currentTargetRegion.IsNotEmpty())
            {
                currentRegionPercent = new Rect
                {
                    X = (double)currentTargetRegion.X / targetSize.Width,
                    Y = (double)currentTargetRegion.Y / targetSize.Height,
                    Height = (double)currentTargetRegion.Height / targetSize.Height,
                    Width = (double)currentTargetRegion.Width / targetSize.Width
                };
            }
            else
            {
                currentRegionPercent = new Rect
                {
                    Width = 1,
                    Height = 1
                };
            }

            var destinationRegion = new Rect
            {
                X = (currentRegionPercent.X + selectionPercent.X * currentRegionPercent.Width) * targetSize.Width,
                Y = (currentRegionPercent.Y + selectionPercent.Y * currentRegionPercent.Height) * targetSize.Height,
                Width = Math.Max(1, currentRegionPercent.Width * selectionPercent.Width * targetSize.Width),
                Height = Math.Max(1, currentRegionPercent.Height * selectionPercent.Height * targetSize.Height)
            };

            Region.SetValue(destinationRegion.ToWinRectangle());
        }

        private struct ThumbnailArgs
        {
            public Thumbnail Thumbnail { get; set; }

            public WinSize ThumbnailSize { get; set; }
            public WinRectangle Source { get; set; }
            public WinRectangle Destination { get; set; }
            public WinSize TargetWindowSize { get; set; }
            public WinRectangle DestinationBounds { get; set; }
            public byte Opacity { get; set; }

            public WindowHandle TargetWindow { get; set; }
            public Size RenderSize { get; set; }

            public override string ToString()
            {
                return $"{nameof(Thumbnail)}(IsInvalid): {Thumbnail}({Thumbnail?.IsInvalid}), {nameof(ThumbnailSize)}: {ThumbnailSize}, {nameof(Source)}: {Source}, {nameof(Destination)}: {Destination}, {nameof(Opacity)}: {Opacity}, {nameof(TargetWindow)}: {TargetWindow}, {nameof(DestinationBounds)}: {DestinationBounds}, {nameof(RenderSize)}: {GeometryExtensions.ToWinSize(RenderSize)}, {nameof(TargetWindowSize)}: {TargetWindowSize}";
            }
        }

        private static byte ToByte(double value)
        {
            if (value < 0)
            {
                value = 0;
            }
            else if (value > 1)
            {
                value = 1;
            }

            return (byte) (value * byte.MaxValue);
        }
    }
}
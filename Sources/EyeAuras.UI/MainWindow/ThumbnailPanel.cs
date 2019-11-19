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
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Scaffolding;
using ReactiveUI;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
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
        private readonly ReplaySubject<Size> renderSizeSource = new ReplaySubject<Size>(1);
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
            
            var thumbnailSource =
                Observable.Merge(
                        this.Observe(OwnerProperty).Select(x => "Owner changed"),
                        this.Observe(IsVisibleProperty).Select(x => "IsVisible changed"),
                        this.Observe(TargetWindowProperty).Select(x => "TargetWindow changed"))
                    .StartWith($"Initial {nameof(CreateThumbnail)} tick")
                    .Select(
                        reason =>
                        {
                            return Observable.Create<Thumbnail>(
                                observer =>
                                {
                                    var args = new ThumbnailArgs()
                                    {
                                        Owner = Owner,
                                        IsVisible = IsVisible,
                                        SourceWindow = TargetWindow
                                    };
                                    Log.Debug($"Recreating thumbnail, reason: {reason}, args: {args}");
                                    
                                    var thumbnailAnchors = new CompositeDisposable();
                                    var result = CreateThumbnail(args);
                                    if (result != null)
                                    {
                                        Disposable.Create(
                                            () =>
                                            {
                                                Log.Debug($"Disposing Thumbnail, args: {args}");
                                                result.Dispose();
                                            }).AddTo(thumbnailAnchors);
                                    }
                                    observer.OnNext(result);
                                    return thumbnailAnchors;
                                });
                        })
                    .Switch();
            
            
            var throttledLogger = new Subject<string>();
            throttledLogger.Sample(UpdateLogSamplingInterval).Subscribe(message => Log.Debug(message)).AddTo(anchors);

            thumbnailSource
                .Select(
                    thumbnail => Observable.Merge(
                            this.Observe(RegionProperty).Select(x => Region.WhenAnyValue(y => y.Bounds)).Switch().DistinctUntilChanged().WithPrevious((prev, curr) => new { prev, curr }).Select(x => $" => RegionBounds changed {x.prev} => {x.curr}"),
                            this.Observe(ThumbnailOpacityProperty).Select(x => ThumbnailOpacity).DistinctUntilChanged().WithPrevious((prev, curr) => new { prev, curr }).Select(x => $" => ThumbnailOpacity changed {x.prev} => {x.curr}"),
                            this.Observe(ThumbnailSizeProperty).Select(x => ThumbnailSize).DistinctUntilChanged().WithPrevious((prev, curr) => new { prev, curr }).Select(x => $" => ThumbnailSize changed {x.prev} => {x.curr}"),
                            renderSizeSource.Select(x => x.ToWinSize()).DistinctUntilChanged().WithPrevious((prev, curr) => new { prev, curr }).Select(x => $" => RenderSize changed {x.prev} => {x.curr}"))
                        .StartWith($"Initial {nameof(UpdateThumbnail)} tick")
                        .Select(
                            reason =>
                            {
                                var args = CanUpdateThumbnail(thumbnail)
                                    ? PrepareUpdateArgs(thumbnail, Region, RenderSize, this, Owner, ThumbnailOpacity)
                                    : default;
                                return new {args, reason};
                            })
                        .DistinctUntilChanged()
                        .Select(x => { 
                            throttledLogger.OnNext($"Updating Thumbnail, reason: {x.reason}, args: {(x.args.Thumbnail == null ? "empty" : x.args.ToString())}");

                            TargetWindowSize = x.args.SourceSize;
                            ThumbnailSize = x.args.SourceRegionSize;

                            UpdateThumbnail(x.args);
                            return x.args;
                        })
                )
                .Switch()
                .RetryWithDelay(RetryInterval, DispatcherScheduler.Current)
                .SubscribeToErrors(Log.HandleUiException)
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

        private static Thumbnail CreateThumbnail(ThumbnailArgs args)
        {
            try
            {
                if (args.SourceWindow == null || args.Owner == null || !args.IsVisible || args.Owner == null)
                {
                    return null;
                }
                
                Log.Debug($"Creating new Thumbnail, targetWindow: {args.SourceWindow}");
                var ownerFormHelper = new WindowInteropHelper(args.Owner);

                var thumbnail = DwmManager.Register(ownerFormHelper.Handle, args.SourceWindow.Handle);
                thumbnail.ShowOnlyClientArea = true;

                return thumbnail;
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to create Thumbnail Handle for window {args.SourceWindow}", e);
                throw;
            }
        }
        
        private static ThumbnailUpdateArgs PrepareUpdateArgs(
            Thumbnail thumbnail,
            ThumbnailRegion sourceRegion,
            Size canvasSize,
            UIElement canvas,
            Window owner,
            double opacity)
        {
            try
            {
                Guard.ArgumentIsTrue(CanUpdateThumbnail(thumbnail), "CanUpdateThumbnail");

                var sourceWindowSize = thumbnail.GetSourceSize();
                var thumbnailSize = sourceRegion == null || sourceRegion.Bounds.IsEmpty || sourceRegion.RegionWidth <= 0 || sourceRegion.RegionHeight <= 0 || !GeometryExtensions.IsNotEmpty(sourceRegion.Bounds)
                    ? sourceWindowSize
                    : sourceRegion.ComputeRegionSize(sourceWindowSize);

                var dpi = VisualTreeHelper.GetDpi(canvas);

                var ownerLocation = canvas.TranslatePoint(new Point(0, 0), owner);
                var location = GeometryExtensions.ToWinPoint(ownerLocation);
                var destination = new Rect(
                    Math.Floor(location.X * dpi.DpiScaleX),
                    Math.Floor(location.Y * dpi.DpiScaleY),
                    Math.Ceiling(canvasSize.Width * dpi.DpiScaleX),
                    Math.Ceiling(canvasSize.Height * dpi.DpiScaleY));

                WinRectangle source;
                var regionBounds = sourceRegion?.Bounds ?? WinRectangle.Empty;
                if (regionBounds.IsEmpty)
                {
                    source = new WinRectangle(0, 0, thumbnailSize.Width, thumbnailSize.Height);
                }
                else
                {
                    source = new WinRectangle(
                        regionBounds.X,
                        regionBounds.Y,
                        regionBounds.Width > 0 && regionBounds.Height > 0
                            ? regionBounds.Width
                            : thumbnailSize.Width,
                        regionBounds.Width > 0 && regionBounds.Height > 0
                            ? regionBounds.Height
                            : thumbnailSize.Height);
                }
                    
                var result = new ThumbnailUpdateArgs
                {
                    DestinationRegion = GeometryExtensions.ToWinRectangle(destination),
                    SourceRegion = source,
                    SourceRegionSize = thumbnailSize,
                    Thumbnail = thumbnail,
                    Opacity = ToByte(opacity),
                    SourceSize = sourceWindowSize,
                };

                return result;
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to build ThumbnailUpdateArgs, args: { new { thumbnail, sourceRegion, opacity } }", e);
                throw;
            }
        }
        
        private static bool CanUpdateThumbnail(Thumbnail thumbnail)
        {
            try
            {
                return thumbnail != null && !thumbnail.IsInvalid;
            }
            catch (Exception e)
            {
                Log.Warn("Failed to check CanUpdateThumbnail", e);
                throw;
            }
        }
        
        private static void UpdateThumbnail(ThumbnailUpdateArgs args)
        {
            try
            {
                if (args.Thumbnail == null)
                {
                    return;
                }
                
                args.Thumbnail.Update(
                    destination: args.DestinationRegion, 
                    source: args.SourceRegion, 
                    opacity: args.Opacity, 
                    visible: true,
                    onlyClientArea: true);
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateThumbnail error, args: {args}", ex);
                throw;
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
            public Window Owner { [CanBeNull] get; [CanBeNull] set; }
            public WindowHandle SourceWindow { [CanBeNull] get; [CanBeNull] set; }
            public bool IsVisible { get; set; }

            public override string ToString()
            {
                return $"{nameof(SourceWindow)}: {SourceWindow}, {nameof(IsVisible)}: {IsVisible}";
            }
        }
        
        private struct ThumbnailUpdateArgs
        {
            public Thumbnail Thumbnail { get; set; }
            public WinSize SourceRegionSize { get; set; }
            public WinSize SourceSize { get; set; }
            public WinRectangle SourceRegion { get; set; }
            public WinRectangle DestinationRegion { get; set; }
            public byte Opacity { get; set; }

            public override string ToString()
            {
                return $"{nameof(Thumbnail)}(IsInvalid): {Thumbnail}({Thumbnail?.IsInvalid}), {nameof(SourceRegionSize)}: {SourceRegionSize}, {nameof(SourceRegion)}: {SourceRegion}, {nameof(DestinationRegion)}: {DestinationRegion},  {nameof(Opacity)}: {Opacity}";
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
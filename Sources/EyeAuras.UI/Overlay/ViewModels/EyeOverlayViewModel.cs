using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using EyeAuras.OnTopReplica;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.Overlay.Views;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;
using Unity;
using Color = System.Windows.Media.Color;
using Size = System.Windows.Size;
using WinSize = System.Drawing.Size;
using WinPoint = System.Drawing.Point;
using WinRectangle = System.Drawing.Rectangle;

namespace EyeAuras.UI.Overlay.ViewModels
{
    internal sealed class EyeOverlayViewModel : OverlayViewModelBase, IEyeOverlayViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EyeOverlayViewModel));
        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;
        private static readonly double DefaultThumbnailOpacity = 1;
        private static readonly double EditModeThumbnailOpacity = 0.5;

        private readonly SerialDisposable activeSelectRegionAnchors = new SerialDisposable();
        private readonly CommandWrapper closeConfigEditorCommand;

        private readonly CommandWrapper fitOverlayCommand;
        private readonly IWindowTracker mainWindowTracker;
        private readonly IOverlayWindowController overlayWindowController;
        private readonly IAuraModelController auraModelController;

        private readonly CommandWrapper resetRegionCommand;
        private readonly CommandWrapper selectRegionCommand;
        private readonly CommandWrapper setAttachedWindowCommand;
        private readonly CommandWrapper setClickThroughCommand;
        private readonly Fallback<double?> thumbnailOpacity = new Fallback<double?>();

        private readonly IWindowListProvider windowListProvider;
        private readonly ObservableAsPropertyHelper<bool> isInEditMode;
        private readonly ObservableAsPropertyHelper<double> aspectRatio;

        private bool maintainAspectRatio = true;
        private bool isContextMenuOpen;
        private WindowHandle attachedWindow;
        private WinSize attachedWindowSize;
        private DpiScale dpi;
        private bool isClickThrough;
        private bool isInSelectMode;
        private string overlayName;
        private WinSize thumbnailSize;
        private Color borderColor;
        private double borderThickness;

        public EyeOverlayViewModel(
            [NotNull] [Dependency(WellKnownWindows.AllWindows)] IWindowTracker mainWindowTracker,
            [NotNull] IOverlayWindowController overlayWindowController,
            [NotNull] IAuraModelController auraModelController,
            [NotNull] IWindowListProvider windowListProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.mainWindowTracker = mainWindowTracker;
            this.overlayWindowController = overlayWindowController;
            this.auraModelController = auraModelController;
            this.windowListProvider = windowListProvider;
            MinSize = new Size(32, 32);
            SizeToContent = SizeToContent.Manual;
            Width = 400;
            Height = 400;
            Left = 200;
            Top = 200;
            IsUnlockable = true;
            Title = "EyeAuras";
            EnableHeader = false;
            thumbnailOpacity.SetDefaultValue(DefaultThumbnailOpacity);

            ResetRegionCommandExecuted();

            WhenLoaded
                .Take(1)
                .Subscribe(ApplyConfig)
                .AddTo(Anchors);

            resetRegionCommand = CommandWrapper.Create(ResetRegionCommandExecuted, ResetRegionCommandCanExecute);
            selectRegionCommand = CommandWrapper.Create(SelectRegionCommandExecuted, SelectRegionCommandCanExecute);
            closeConfigEditorCommand = CommandWrapper.Create(CloseConfigEditorCommandExecuted);
            fitOverlayCommand = CommandWrapper.Create<double?>(FitOverlayCommandExecuted);
            setAttachedWindowCommand = CommandWrapper.Create<WindowHandle>(SetAttachedWindowCommandExecuted);
            setClickThroughCommand = CommandWrapper.Create<bool?>(SetClickThroughModeExecuted);
            DisableAuraCommand = CommandWrapper.Create(() => auraModelController.IsEnabled = false);
            CloseCommand = CommandWrapper.Create(CloseCommandExecuted, auraModelController.WhenAnyValue(x => x.CloseController).Select(CloseCommandCanExecute));

            auraModelController.WhenAnyValue(x => x.Name)
                .Where(x => !string.IsNullOrEmpty(x))
                .Subscribe(x => OverlayName = x)
                .AddTo(Anchors);

            this.RaiseWhenSourceValue(x => x.ActiveThumbnailOpacity, thumbnailOpacity, x => x.Value).AddTo(Anchors);
            this.RaiseWhenSourceValue(x => x.ThumbnailOpacity, this, x => x.ActiveThumbnailOpacity).AddTo(Anchors);

            isInEditMode = Observable.Merge(
                    this.WhenAnyProperty(x => x.IsInSelectMode, x => x.IsLocked))
                .Select(change => IsInSelectMode || !IsLocked)
                .ToPropertyHelper(this, x => x.IsInEditMode, uiScheduler)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsLocked)
                .Where(x => x && isInSelectMode)
                .Subscribe(() => IsInSelectMode = false)
                .AddTo(Anchors);

            aspectRatio = this.WhenAnyProperty(x => x.Bounds, x => x.ViewModelLocation)
                .Select(change => Width >= 0 && Height >= 0
                    ? Width / Height
                    : double.PositiveInfinity)
                .ToPropertyHelper(this, x => x.AspectRatio, uiScheduler)
                .AddTo(Anchors);
        }

        private bool CloseCommandCanExecute()
        {
            return auraModelController.CloseController != null;
        }

        private void CloseCommandExecuted()
        {
            Guard.ArgumentIsTrue(CloseCommandCanExecute(), "CloseCommand can execute");
            
            Log.Debug($"Closing Overlay {OverlayName}");
            auraModelController.CloseController.Close();
        }

        public bool IsInEditMode => isInEditMode.Value;

        public bool IsInSelectMode
        {
            get => isInSelectMode;
            set => RaiseAndSetIfChanged(ref isInSelectMode, value);
        }

        public Color BorderColor
        {
            get => borderColor;
            set => this.RaiseAndSetIfChanged(ref borderColor, value);
        }

        public double BorderThickness
        {
            get => borderThickness;
            set => this.RaiseAndSetIfChanged(ref borderThickness, value);
        }
        
        public ReadOnlyObservableCollection<WindowHandle> WindowList => windowListProvider.WindowList;

        public ICommand CloseConfigEditorCommand => closeConfigEditorCommand;

        public ICommand SetClickThroughCommand => setClickThroughCommand;

        public ICommand DisableAuraCommand { get; }
        
        public ICommand CloseCommand { get; }
        
        public double? ScaleRatioHalf => 1 / 2f;

        public double? ScaleRatioQuarter => 1 / 4f;

        public double? ScaleRatioActual => 1f;

        public double? ScaleRatioDouble => 2f;

        public double ActiveThumbnailOpacity => thumbnailOpacity.Value ?? DefaultThumbnailOpacity;

        public WinSize ThumbnailSize
        {
            get => thumbnailSize;
            set => RaiseAndSetIfChanged(ref thumbnailSize, value);
        }

        public WinSize AttachedWindowSize
        {
            get => attachedWindowSize;
            set => RaiseAndSetIfChanged(ref attachedWindowSize, value);
        }

        public double AspectRatio => aspectRatio.Value;

        public ICommand SelectRegionCommand => selectRegionCommand;

        public ICommand SetAttachedWindowCommand => setAttachedWindowCommand;

        public ICommand FitOverlayCommand => fitOverlayCommand;

        public bool MaintainAspectRatio
        {
            get => maintainAspectRatio;
            set => RaiseAndSetIfChanged(ref maintainAspectRatio, value);
        }

        public void ScaleOverlay(double scaleRatio)
        {
            if ( !ThumbnailSize.IsNotEmpty())
            {
                throw new InvalidOperationException("ThumbnailSize must be defined before Scaling");
            }
            var currentSize = new Size(Width, Height);
            var newSize = new Size(
                ThumbnailSize.Width * scaleRatio,
                ThumbnailSize.Height * scaleRatio
            ).Scale(1f / dpi.DpiScaleX, 1f / dpi.DpiScaleY);

            Log.Debug($"ScaleOverlay({scaleRatio}) executed, sizing args: {new {currentSize, newSize, thumbnailSize, dpi}}");

            Width = newSize.Width;
            Height = newSize.Height;
            
            Log.Debug($"ScaleOverlay({scaleRatio}) resized window, bounds: {Bounds} (native: {NativeBounds})");
        }

        public double ThumbnailOpacity
        {
            get => thumbnailOpacity.DefaultValue ?? DefaultThumbnailOpacity;
            set => thumbnailOpacity.SetDefaultValue(value);
        }

        public WindowHandle AttachedWindow
        {
            get => attachedWindow;
            set => RaiseAndSetIfChanged(ref attachedWindow, value);
        }

        public string OverlayName
        {
            get => overlayName;
            private set => RaiseAndSetIfChanged(ref overlayName, value);
        }

        public bool IsClickThrough
        {
            get => isClickThrough;
            set => RaiseAndSetIfChanged(ref isClickThrough, value);
        }

        public ThumbnailRegion Region { get; } = new ThumbnailRegion(Rectangle.Empty);

        public ICommand ResetRegionCommand => resetRegionCommand;

        private void SetClickThroughModeExecuted(bool? value)
        {
            IsClickThrough = value ?? false;
        }

        private void CloseConfigEditorCommandExecuted()
        {
            activeSelectRegionAnchors.Disposable = null;
        }

        private bool ResetRegionCommandCanExecute()
        {
            return AttachedWindow != null;
        }

        private bool SelectRegionCommandCanExecute()
        {
            return AttachedWindow != null && !IsInSelectMode;
        }

        private void SelectRegionCommandExecuted()
        {
            var selectRegionAnchors = OpenConfigEditor();
            Log.Debug($"Region selection mode turned on, Region: {Region}");
            IsInSelectMode = true;

            Disposable.Create(() => IsInSelectMode = false).AddTo(selectRegionAnchors);

            this.WhenAnyValue(x => x.IsInSelectMode)
                .Where(x => !x)
                .Subscribe(CloseConfigEditorCommandExecuted)
                .AddTo(selectRegionAnchors);
        }

        private void SetAttachedWindowCommandExecuted(WindowHandle obj)
        {
            AttachedWindow = obj;
        }

        private void FitOverlayCommandExecuted(double? scaleRatio)
        {
            if (scaleRatio == null || !ThumbnailSize.IsNotEmpty())
            {
                return;
            }
            ScaleOverlay(scaleRatio.Value);
        }

        private void HandleInitialWindowAttachment()
        {
            Title = $"Overlay {AttachedWindow.Title}";
        }

        private CompositeDisposable OpenConfigEditor()
        {
            var anchors = new CompositeDisposable();
            activeSelectRegionAnchors.Disposable = anchors;

            var window = new OverlayConfigEditor
            {
                DataContext = this
            };
            Disposable.Create(
                    () =>
                    {
                        Log.Debug("Closing ConfigEditor");
                        window.Close();
                    })
                .AddTo(anchors);

            window.Left = OverlayWindow.Left + OverlayWindow.ActualWidth + 10;
            window.Top = OverlayWindow.Top;

            overlayWindowController.WhenAnyValue(x => x.IsVisible)
                .Subscribe(
                    x =>
                    {
                        if (x)
                        {
                            window.Show();
                        }
                        else
                        {
                            window.Hide();
                        }
                    })
                .AddTo(anchors);

            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => window.Closed += h, h => window.Closed -= h)
                .Subscribe(CloseConfigEditorCommandExecuted)
                .AddTo(anchors);

            return anchors;
        }

        private void ResetRegionCommandExecuted()
        {
            IsInSelectMode = false;
            Region.SetValue(Rectangle.Empty);
        }

        private void ApplyConfig()
        {
            dpi = VisualTreeHelper.GetDpi(OverlayWindow);

            mainWindowTracker.WhenAnyValue(x => x.ActiveProcessId)
                .ToUnit()
                .Merge(this.WhenAnyProperty(x => x.IsInSelectMode, x => x.IsLocked, x => x.IsClickThrough).ToUnit())
                .Select(
                    x => new
                    {
                        IsUnclocked = !IsLocked,
                        IsSelectedInMainWindow = mainWindowTracker.ActiveProcessId == CurrentProcessId,
                        IsNotClickThroughByDefault = !IsClickThrough
                    })
                .DistinctUntilChanged()
                .Select(
                    x => x.IsUnclocked || x.IsSelectedInMainWindow || x.IsNotClickThroughByDefault
                        ? OverlayMode.Layered
                        : OverlayMode.Transparent)
                .DistinctUntilChanged()
                .Subscribe(x => OverlayMode = x)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.AttachedWindow)
                .Where(x => x != null)
                .Subscribe(HandleInitialWindowAttachment)
                .AddTo(Anchors);

            Observable.Merge(this.WhenAnyValue(x => x.ThumbnailSize).ToUnit(), this.WhenAnyValue(x => x.MaintainAspectRatio).ToUnit())
                .Where(x => thumbnailSize.IsNotEmpty())
                .Select(x => thumbnailSize)
                .Subscribe(HandleSourceSizeChange)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.AttachedWindow)
                .Subscribe(() => resetRegionCommand.RaiseCanExecuteChanged())
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsInSelectMode, x => x.AttachedWindow)
                .Subscribe(() => selectRegionCommand.RaiseCanExecuteChanged())
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsInEditMode)
                .Subscribe(
                    x => thumbnailOpacity.SetValue(
                        x
                            ? EditModeThumbnailOpacity
                            : default(double?)))
                .AddTo(Anchors);
        }

        private void HandleSourceSizeChange(WinSize sourceSize)
        {
            if (!GeometryExtensions.IsNotEmpty(sourceSize))
            {
                throw new ApplicationException($"SourceSize must be non-empty at this point, got {sourceSize} (maintainAspectRatio: {MaintainAspectRatio})");
            }

            double? aspectRatio = (double)sourceSize.Width / sourceSize.Height;
            if (maintainAspectRatio)
            {
                Log.Debug($"Handling Source size change: {sourceSize}, aspectRatio: {TargetAspectRatio} => {aspectRatio}");
                TargetAspectRatio = aspectRatio;
            }
            else
            {
                Log.Debug($"Handling Source size change: {sourceSize}, aspect ratio sync is disabled, source AspectRatio: {aspectRatio}");
                TargetAspectRatio = null;
            }
        }

        public override string ToString()
        {
            return $"[{OverlayName}]";
        }
    }
}
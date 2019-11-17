using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using EyeAuras.OnTopReplica;
using EyeAuras.Shared.Services;
using EyeAuras.UI.RegionSelector.Services;
using PoeShared.Native;
using PoeShared.Scaffolding;
using ReactiveUI;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Input;
using EyeAuras.OnTopReplica.WindowSeekers;
using JetBrains.Annotations;
using log4net;
using PoeShared.Prism;
using Unity;
using Point = System.Drawing.Point;

namespace EyeAuras.UI.RegionSelector.ViewModels
{
    public sealed class RegionSelectorViewModel : DisposableReactiveObject, IRegionSelectorViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RegionSelectorViewModel));
        private static readonly TimeSpan ThrottlingPeriod = TimeSpan.FromMilliseconds(100);
        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;

        private Rectangle selection;
        private Point mouseLocation;
        private RegionSelectorResult mouseRegion;
        private readonly IWindowSeeker windowSeeker;

        public RegionSelectorViewModel(
            [NotNull] IKeyboardEventsSource eventListener,
            [NotNull] ICloseController<RegionSelectorResult> closeController,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            windowSeeker = new TaskWindowSeeker()
            {
                SkipNotVisibleWindows = true
            };
            eventListener.InitializeMouseHook().AddTo(Anchors);
            eventListener.WhenMouseMove
                .Select(x => x.Location)
                .StartWith(System.Windows.Forms.Cursor.Position)
                .ObserveOn(uiScheduler)
                .Subscribe(x => MouseLocation = x)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.MouseLocation)
                .Sample(ThrottlingPeriod)
                .Select(x => new Rectangle(x.X, x.Y, 1, 1))
                .Select(ToRegionResult)
                .ObserveOn(uiScheduler)
                .Subscribe(x => MouseRegion = x)
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.Selection)
                .Skip(1)
                .Subscribe(
                    screenRegion =>
                    {
                        var result = ToRegionResult(screenRegion);
                        closeController.Close(result);
                    })
                .AddTo(Anchors);
            
            Observable.Timer(DateTimeOffset.Now, TimeSpan.FromSeconds(1))
                .Subscribe(() => windowSeeker.Refresh())
                .AddTo(Anchors);
        }

        public Rectangle Selection
        {
            get => selection;
            set => this.RaiseAndSetIfChanged(ref selection, value);
        }

        public RegionSelectorResult MouseRegion
        {
            get => mouseRegion;
            set => this.RaiseAndSetIfChanged(ref mouseRegion, value);
        }

        public Point MouseLocation
        {
            get => mouseLocation;
            set => this.RaiseAndSetIfChanged(ref mouseLocation, value);
        }

        private RegionSelectorResult ToRegionResult(Rectangle screenRegion)
        {
            if (screenRegion.IsEmpty)
            {
                return new RegionSelectorResult { Reason = "Selected Empty screen region" };
            }
            
            var (window, selection) = FindMatchingWindow(screenRegion, windowSeeker.Windows);

            if (window != null)
            {
                var absoluteSelection = selection;
                absoluteSelection.Offset(window.ClientBounds.Left, window.ClientBounds.Top);
                return new RegionSelectorResult()
                {
                    AbsoluteSelection = absoluteSelection,
                    Selection = selection,
                    Window = window,
                    Reason = "OK"
                };
            }
            
            return new RegionSelectorResult { Reason = $"Could not find matching window in region {screenRegion}" };
        }
        
        private static (WindowHandle window, Rectangle selection) FindMatchingWindow(Rectangle selection, IEnumerable<WindowHandle> windows)
        {
            var topLeft = new Point(selection.Left, selection.Top);
            var intersections = windows
                .Where(x => x.ProcessId != CurrentProcessId)
                .Where(x => UnsafeNative.WindowIsVisible(x.Handle))
                .Where(x => x.ClientBounds.IsNotEmpty())
                .Where(x => x.ClientBounds.Contains(topLeft))
                .Select(
                    (x, idx) =>
                    {
                        var intersection = x.ClientBounds;
                        if (selection.Width > 0 && selection.Height > 0)
                        {
                            intersection.Intersect(selection);
                            intersection.Offset(-x.ClientBounds.Left, -x.ClientBounds.Top);
                        }
                        else
                        {
                            intersection = new Rectangle(0, 0, intersection.Width, intersection.Height);
                        }
                        
                        return new
                        {
                            Window = x,
                            Intersection = intersection,
                            Area = intersection.Width * intersection.Height
                        };
                    })
                .Where(x => GeometryExtensions.IsNotEmpty(x.Intersection))
                .OrderBy(x => x.Window.ZOrder)
                .ToArray();

            var topmostHandle = UnsafeNative.GetTopmostHwnd(intersections.Select(x => x.Window.Handle).ToArray());
            var result = intersections.FirstOrDefault(x => x.Window.Handle == topmostHandle);

            return result == null
                ? (null, Rectangle.Empty)
                : (result.Window, result.Intersection);
        }
    }
}
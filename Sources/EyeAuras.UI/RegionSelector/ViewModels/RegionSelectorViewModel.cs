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
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using EyeAuras.OnTopReplica.WindowSeekers;
using JetBrains.Annotations;
using log4net;
using PoeShared.Prism;
using Unity;
using Point = System.Drawing.Point;
using Size = System.Windows.Size;

using WinSize = System.Drawing.Size;
using WinPoint = System.Drawing.Point;
using WinRectangle = System.Drawing.Rectangle;

namespace EyeAuras.UI.RegionSelector.ViewModels
{
    internal sealed class RegionSelectorViewModel : DisposableReactiveObject, IRegionSelectorViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RegionSelectorViewModel));
        private static readonly TimeSpan ThrottlingPeriod = TimeSpan.FromMilliseconds(250);
        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;
        private static readonly double MinSelectionArea = 20;

        private RegionSelectorResult selectionCandidate;
        private readonly IWindowSeeker windowSeeker;

        public RegionSelectorViewModel(
            [NotNull] ISelectionAdornerViewModel selectionAdorner,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            SelectionAdorner = selectionAdorner.AddTo(Anchors);
            windowSeeker = new TaskWindowSeeker
            {
                SkipNotVisibleWindows = true
            };

            var refreshRequest = new Subject<Unit>();

            SelectionAdorner.WhenAnyValue(x => x.MousePosition, x => x.Owner).ToUnit()
                .Merge(refreshRequest)
                .Select(x => new { SelectionAdorner.MousePosition, SelectionAdorner.Owner })
                .Where(x => x.Owner != null)
                .Sample(ThrottlingPeriod, bgScheduler)
                .ObserveOn(uiScheduler)
                .Select(x => x.MousePosition.ToScreen(x.Owner))
                .Select(x => new Rectangle(x.X, x.Y, 1, 1))
                .Select(ToRegionResult)
                .Do(x => Log.Debug($"Selection candidate: {x}"))
                .Subscribe(x => SelectionCandidate = x)
                .AddTo(Anchors);
            
            refreshRequest
                .Subscribe(() => windowSeeker.Refresh())
                .AddTo(Anchors);

            Observable.Timer(DateTimeOffset.Now, TimeSpan.FromSeconds(1), bgScheduler).ToUnit()
                .Subscribe(refreshRequest)
                .AddTo(Anchors);
        }

        public ISelectionAdornerViewModel SelectionAdorner { get; }
        public IObservable<RegionSelectorResult> SelectWindow()
        {
            return SelectionAdorner.StartSelection()
                .Select(x => x.ScaleToScreen())
                .Do(x => Log.Debug($"Selected region: {x}"))
                .Select(x => x.Width * x.Height >= MinSelectionArea ? x : new WinRectangle(x.X, x.Y, 0 , 0))
                .Select(ToRegionResult)
                .Do(x => Log.Debug($"Selection Result: {x}"));
        }

        public RegionSelectorResult SelectionCandidate
        {
            get => selectionCandidate;
            private set => this.RaiseAndSetIfChanged(ref selectionCandidate, value);
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
                return new RegionSelectorResult
                {
                    AbsoluteSelection = absoluteSelection,
                    Selection = selection,
                    Window = window,
                    Reason = "OK"
                };
            }
            
            return new RegionSelectorResult { Reason = $"Could not find matching window in region {screenRegion}" };
        }
        
        private static (WindowHandle window, Rectangle selection) FindMatchingWindow(Rectangle selection, ICollection<WindowHandle> windows)
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
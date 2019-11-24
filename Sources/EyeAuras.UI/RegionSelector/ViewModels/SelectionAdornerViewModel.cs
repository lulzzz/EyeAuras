using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Gma.System.MouseKeyHook;
using JetBrains.Annotations;
using log4net;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using WinSize = System.Drawing.Size;
using WinPoint = System.Drawing.Point;
using WinRectangle = System.Drawing.Rectangle;

namespace EyeAuras.UI.RegionSelector.ViewModels
{
    internal sealed class SelectionAdornerViewModel : DisposableReactiveObject, ISelectionAdornerViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SelectionAdornerViewModel));

        private readonly IKeyboardEventsSource keyboardEventsSource;
        private readonly IScheduler uiScheduler;
        private readonly DoubleCollection lineDashArray = new DoubleCollection {2, 2};
        private readonly MouseButtons mouseSelectionButton = MouseButtons.Left;

        private Point anchorPoint;
        private Point mousePosition;
        private Rect selection;
        private bool isVisible;
        private UIElement owner;

        public SelectionAdornerViewModel(
            [NotNull] IKeyboardEventsSource keyboardEventsSource,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.keyboardEventsSource = keyboardEventsSource;
            this.uiScheduler = uiScheduler;
            this.WhenAnyProperty(x => x.Selection, x => x.MousePosition, x => x.Owner)
                .Where(x => Owner != null)
                .Subscribe(Redraw)
                .AddTo(Anchors);
        }

        public Rect Selection
        {
            get => selection;
            private set => RaiseAndSetIfChanged(ref selection, value);
        }

        public bool IsVisible
        {
            get => isVisible;
            private set => RaiseAndSetIfChanged(ref isVisible, value);
        }

        public ObservableCollection<Shape> CanvasElements { get; } = new ObservableCollection<Shape>();

        public double StrokeThickness { get; } = 2;

        public Brush Stroke { get; } = Brushes.Lime;

        public Point AnchorPoint
        {
            get => anchorPoint;
            private set => RaiseAndSetIfChanged(ref anchorPoint, value);
        }

        public Point MousePosition
        {
            get => mousePosition;
            private set => RaiseAndSetIfChanged(ref mousePosition, value);
        }

        public UIElement Owner
        {
            get => owner;
            set => RaiseAndSetIfChanged(ref owner, value);
        }

        public IObservable<Rect> StartSelection()
        {
            return Observable.Create<Rect>(
                subscriber =>
                {
                    IsVisible = true;

                    var selectionAnchors = new CompositeDisposable();
                    Disposable.Create(() => Log.Debug($"Disposing SelectionAnchors")).AddTo(selectionAnchors);
                    Disposable.Create(() => IsVisible = false).AddTo(selectionAnchors);
                    Selection = Rect.Empty;
                    keyboardEventsSource.InitializeMouseHook().AddTo(selectionAnchors);

                    Observable.Merge(
                            keyboardEventsSource.WhenKeyUp.Where(x => x.KeyData == Keys.Escape).Select(x => $"{x.KeyData} pressed"),
                            keyboardEventsSource.WhenMouseUp.Where(x => x.Button != mouseSelectionButton).Select(x => $"mouse {x.Button} pressed"))
                        .ObserveOn(uiScheduler)
                        .Do(reason => Log.Info($"Closing SelectionAdorner, reason: {reason}"))
                        .Subscribe(subscriber.OnCompleted)
                        .AddTo(selectionAnchors);
                    
                    keyboardEventsSource.WhenMouseMove
                        .Subscribe(HandleMouseMove)
                        .AddTo(selectionAnchors);

                    keyboardEventsSource
                        .WhenMouseDown
                        .Where(x => x.Button == mouseSelectionButton)
                        .ObserveOn(uiScheduler)
                        .Select(x =>
                        {
                            var coords = owner.PointFromScreen(new Point(x.X, x.Y));
                            AnchorPoint = coords;
                            Selection = new Rect(anchorPoint.X, anchorPoint.Y, 0, 0);
                            return keyboardEventsSource.WhenMouseUp.Where(y => y.Button == mouseSelectionButton);
                        })
                        .Switch()
                        .ObserveOn(uiScheduler)
                        .Select(
                            x =>
                            {
                                var result = Selection;
                                Selection = Rect.Empty;

                                if (result.Width * result.Height < 20)
                                {
                                    result = new Rect(mousePosition.X, mousePosition.Y, 0, 0);
                                }

                                return Observable.Return(result);
                            })
                        .Switch()
                        .Subscribe(subscriber)
                        .AddTo(selectionAnchors);

                    return selectionAnchors;
                });
        }

        private void HandleMouseMove(MouseEventArgs e)
        {
            var coords = owner.PointFromScreen(new Point(e.X, e.Y));
            var renderSize = owner.RenderSize;
            MousePosition = new Point(
                Math.Max(0, Math.Min(coords.X, renderSize.Width)),
                Math.Max(0, Math.Min(coords.Y, renderSize.Height)));
            
            if (e.Button == mouseSelectionButton)
            {
                var destinationRect = new Rect(0, 0, renderSize.Width, renderSize.Height);

                var newSelection = new Rect
                {
                    X = mousePosition.X < anchorPoint.X
                        ? mousePosition.X
                        : anchorPoint.X,
                    Y = mousePosition.Y < anchorPoint.Y
                        ? mousePosition.Y
                        : anchorPoint.Y,
                    Width = Math.Abs(mousePosition.X - anchorPoint.X),
                    Height = Math.Abs(mousePosition.Y - anchorPoint.Y)
                };
                newSelection.Intersect(destinationRect);
                Selection = newSelection;
            }
            else
            {
                Selection = new Rect(AnchorPoint, Size.Empty);
            }
        }

        private void Redraw()
        {
            CanvasElements.Clear();

            var adornedElementSize = owner.RenderSize;
            var destinationRect = new Rect(0, 0, adornedElementSize.Width, adornedElementSize.Height);

            if (selection.IsNotEmpty())
            {
                new Line {X1 = selection.TopLeft.X, X2 = selection.TopLeft.X, Y1 = 0, Y2 = selection.TopLeft.Y}.AddTo(
                    CanvasElements);
                new Line {X1 = selection.TopRight.X, X2 = selection.TopRight.X, Y1 = 0, Y2 = selection.TopRight.Y}
                    .AddTo(CanvasElements);
                new Line {X1 = 0, X2 = selection.TopLeft.X, Y1 = selection.TopLeft.Y, Y2 = selection.TopLeft.Y}.AddTo(
                    CanvasElements);
                new Line
                {
                    X1 = selection.TopRight.X, X2 = destinationRect.Right, Y1 = selection.TopRight.Y,
                    Y2 = selection.TopRight.Y
                }.AddTo(CanvasElements);

                new Line {X1 = 0, X2 = selection.BottomLeft.X, Y1 = selection.BottomLeft.Y, Y2 = selection.BottomLeft.Y}
                    .AddTo(CanvasElements);
                new Line
                {
                    X1 = selection.BottomRight.X, X2 = destinationRect.Right, Y1 = selection.BottomRight.Y,
                    Y2 = selection.BottomRight.Y
                }.AddTo(CanvasElements);
                new Line
                {
                    X1 = selection.BottomLeft.X, X2 = selection.BottomLeft.X, Y1 = selection.BottomLeft.Y,
                    Y2 = destinationRect.Bottom
                }.AddTo(CanvasElements);
                new Line
                {
                    X1 = selection.BottomRight.X, X2 = selection.BottomRight.X, Y1 = selection.BottomRight.Y,
                    Y2 = destinationRect.Bottom
                }.AddTo(CanvasElements);

                var selectionRect = new Rectangle
                {
                    Width = selection.Width,
                    Height = selection.Height,
                    StrokeThickness = 1,
                    Stroke = Stroke
                }.AddTo(CanvasElements);

                Canvas.SetLeft(selectionRect, selection.Left);
                Canvas.SetTop(selectionRect, selection.Top);
            }
            else
            {
                new Line {X1 = 0, X2 = destinationRect.Width, Y1 = mousePosition.Y, Y2 = mousePosition.Y}.AddTo(
                    CanvasElements);
                new Line {X1 = mousePosition.X, X2 = mousePosition.X, Y1 = 0, Y2 = destinationRect.Height}.AddTo(
                    CanvasElements);
            }

            foreach (var line in CanvasElements.OfType<Line>())
            {
                line.SetCurrentValue(Shape.StrokeProperty, Stroke);
                line.SetCurrentValue(Shape.StrokeThicknessProperty, StrokeThickness);
                line.SetCurrentValue(Shape.StrokeDashArrayProperty, lineDashArray);
            }
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using log4net;
using PoeShared.Scaffolding;
using ReactiveUI;
using WinSize = System.Drawing.Size;
using WinPoint = System.Drawing.Point;
using WinRectangle = System.Drawing.Rectangle;

namespace EyeAuras.UI.RegionSelector.ViewModels
{
    internal sealed class SelectionAdornerViewModel : DisposableReactiveObject, ISelectionAdornerViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SelectionAdornerViewModel));

        private readonly DoubleCollection lineDashArray = new DoubleCollection {2, 2};

        private bool isVisible;

        private Point mousePosition;

        private UIElement owner;

        private Rect selection;

        public SelectionAdornerViewModel()
        {
            this.WhenAnyValue(x => x.Owner)
                .Where(x => x != null)
                .Take(1)
                .Subscribe(HandleOwnerChanged)
                .AddTo(Anchors);

            this.WhenAnyProperty(x => x.Selection, x => x.MousePosition, x => x.Owner)
                .Where(x => Owner != null)
                .Subscribe(Redraw)
                .AddTo(Anchors);
        }

        public double StrokeThickness { get; } = 2;

        public Brush Stroke { get; } = Brushes.Lime;

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

        private Point anchorPoint;

        public Point AnchorPoint
        {
            get => anchorPoint;
            private set => this.RaiseAndSetIfChanged(ref anchorPoint, value);
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

        public ObservableCollection<Shape> CanvasElements { get; } = new ObservableCollection<Shape>();

        private void HandleOwnerChanged(UIElement owner)
        {
            Log.Info($"Owner: {owner}");
        }

        public IObservable<Rect> StartSelection()
        {
            return Observable.Create<Rect>(
                subscriber =>
                {
                    IsVisible = true;

                    var selectionAnchors = new CompositeDisposable();
                    Selection = Rect.Empty;

                    owner.Observe(UIElement.IsMouseOverProperty)
                        .StartWithDefault()
                        .Select(x => owner.IsMouseOver)
                        .Subscribe(
                            x => Log.Info($"IsMouseOver: {x}"))
                        .AddTo(selectionAnchors);

                    Observable
                        .FromEventPattern<MouseEventHandler, MouseEventArgs>(
                            h => owner.MouseMove += h,
                            h => owner.MouseMove -= h)
                        .Select(x => x.EventArgs)
                        .Subscribe(HandleMouseMove)
                        .AddTo(selectionAnchors);

                    var mouseDownEvents = Observable
                        .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
                            h => owner.MouseDown += h,
                            h => owner.MouseDown -= h)
                        .Select(x => x.EventArgs);

                    mouseDownEvents
                        .Subscribe(
                            e =>
                            {
                                AnchorPoint = e.GetPosition(Owner);
                                Selection = new Rect(anchorPoint.X, anchorPoint.Y, 0, 0);
                            })
                        .AddTo(selectionAnchors);

                    Observable
                        .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
                            h => owner.MouseUp += h,
                            h => owner.MouseUp -= h)
                        .Select(x => x.EventArgs)
                        .SkipUntil(mouseDownEvents)
                        .Select(
                            x =>
                            {
                                x.Handled = true;

                                var result = Selection;
                                Selection = Rect.Empty;
                                if (x.ChangedButton != MouseButton.Left) return Observable.Return(Rect.Empty);

                                if (result.Width * result.Height < 20)
                                {
                                    result = new Rect(mousePosition.X, mousePosition.Y, 0, 0);
                                }
                                return Observable.Return(result);
                            })
                        .Switch()
                        .Finally(() => { IsVisible = false; })
                        .Subscribe(subscriber)
                        .AddTo(selectionAnchors);

                    return selectionAnchors;
                });
        }

        private void HandleMouseMove(MouseEventArgs e)
        {
            MousePosition = ToMousePosition(e, owner);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var renderSize = owner.RenderSize;
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

        private static Point ToMousePosition(MouseEventArgs e, UIElement owner)
        {
            var mousePosition = e.GetPosition(owner);
            var renderSize = owner.RenderSize;
            mousePosition.X = Math.Min(renderSize.Width, Math.Max(0, mousePosition.X));
            mousePosition.Y = Math.Min(renderSize.Height, Math.Max(0, mousePosition.Y));
            return mousePosition;
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
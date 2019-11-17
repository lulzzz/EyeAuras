using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using PoeShared;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.MainWindow
{
    public class SelectionAdorner : Adorner
    {
        public static readonly DependencyProperty SelectionProperty = DependencyProperty.Register(
            "Selection",
            typeof(Rect),
            typeof(SelectionAdorner),
            new PropertyMetadata(default(Rect)));

        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            "Stroke",
            typeof(Brush),
            typeof(SelectionAdorner),
            new PropertyMetadata(default(Brush)));

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            "StrokeThickness",
            typeof(double),
            typeof(SelectionAdorner),
            new PropertyMetadata((double) 1));

        private readonly Canvas canvas;
        private readonly Grid content;
        private readonly DoubleCollection lineDashArray = new DoubleCollection {2, 2};

        private readonly UIElement owner;

        private AdornerLayer adornerLayer;

        public SelectionAdorner(UIElement owner)
            : base(owner)
        {
            this.owner = owner;

            canvas = new Canvas
            {
                IsHitTestVisible = false
            };

            content = new Grid();
            content.Children.Add(canvas);

            AddVisualChild(content);


            Loaded += OnLoaded;
        }

        public double StrokeThickness
        {
            get => (double) GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public Rect Selection
        {
            get => (Rect) GetValue(SelectionProperty);
            set => SetValue(SelectionProperty, value);
        }

        public Brush Stroke
        {
            get => (Brush) GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        protected override int VisualChildrenCount => content.Children.Count;


        public IObservable<Rect> StartSelection()
        {
            return Observable.Create<Rect>(
                subscriber =>
                {
                    Visibility = Visibility.Visible;

                    var selectionAnchors = new CompositeDisposable();
                    var anchorPoint = new Point();

                    owner.Observe(IsMouseOverProperty)
                        .StartWithDefault()
                        .Select(x => owner.IsMouseOver)
                        .Subscribe(
                            x => content.Visibility = x
                                ? Visibility.Visible
                                : Visibility.Hidden)
                        .AddTo(selectionAnchors);

                    Observable
                        .FromEventPattern<MouseEventHandler, MouseEventArgs>(h => owner.MouseMove += h, h => owner.MouseMove -= h)
                        .Select(x => x.EventArgs)
                        .Subscribe(x => HandleMouseMove(anchorPoint, x))
                        .AddTo(selectionAnchors);

                    var mouseDownEvents = Observable
                        .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(h => owner.MouseDown += h, h => owner.MouseDown -= h)
                        .Select(x => x.EventArgs);

                    mouseDownEvents
                        .Subscribe(
                            e =>
                            {
                                anchorPoint = e.GetPosition(this);
                                Selection = new Rect(anchorPoint.X, anchorPoint.Y, 0, 0);
                            })
                        .AddTo(selectionAnchors);

                    Observable
                        .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(h => owner.MouseUp += h, h => owner.MouseUp -= h)
                        .Select(x => x.EventArgs)
                        .SkipUntil(mouseDownEvents)
                        .Select(
                            x =>
                            {
                                x.Handled = true;

                                var mousePosition = ToMousePosition(x);
                                var result = Selection;
                                Selection = Rect.Empty;
                                if (x.ChangedButton != MouseButton.Left)
                                {
                                    return Observable.Return(Rect.Empty);
                                }

                                if (result.Width * result.Height < 20)
                                {
                                    result = new Rect(mousePosition.X, mousePosition.Y, 0, 0);
                                }
                                return Observable.Return(result);
                            })
                        .Switch()
                        .Take(1)
                        .Finally(() => { Visibility = Visibility.Collapsed; })
                        .Subscribe(subscriber)
                        .AddTo(selectionAnchors);
                    
                    owner.Observe(SelectionProperty)
                        .Subscribe(
                            () =>
                            {
                                var mousePosition = InputManager.Current.PrimaryMouseDevice.GetPosition(owner);
                                Redraw(mousePosition, Selection);
                            })
                        .AddTo(selectionAnchors);

                    return selectionAnchors;
                });
        }

        private void HandleMouseMove(Point anchorPoint, MouseEventArgs e)
        {
            var mousePosition = ToMousePosition(e);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var destinationRect = new Rect(0, 0, RenderSize.Width, RenderSize.Height);

                var selection = new Rect
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

                selection.Intersect(destinationRect);
                Selection = selection;
            }

            Redraw(mousePosition, Selection);
        }

        private Point ToMousePosition(MouseEventArgs e)
        {
            var mousePosition = e.GetPosition(owner);
            mousePosition.X = Math.Min(RenderSize.Width, Math.Max(0, mousePosition.X));
            mousePosition.Y = Math.Min(RenderSize.Height, Math.Max(0, mousePosition.Y));
            return mousePosition;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            adornerLayer = AdornerLayer.GetAdornerLayer(owner);
            Guard.ArgumentNotNull(adornerLayer, nameof(adornerLayer));
        }

        protected override Size ArrangeOverride(Size size)
        {
            var finalSize = base.ArrangeOverride(size);
            ((UIElement) GetVisualChild(0))?.Arrange(new Rect(new Point(), finalSize));
            return finalSize;
        }

        private void Redraw(Point mousePosition, Rect selection)
        {
            canvas.Children.Clear();

            var adornedElementSize = owner.RenderSize;
            var destinationRect = new Rect(0, 0, adornedElementSize.Width, adornedElementSize.Height);

            if (GeometryExtensions.IsNotEmpty(selection))
            {
                new Line {X1 = selection.TopLeft.X, X2 = selection.TopLeft.X, Y1 = 0, Y2 = selection.TopLeft.Y}.AddTo(canvas);
                new Line {X1 = selection.TopRight.X, X2 = selection.TopRight.X, Y1 = 0, Y2 = selection.TopRight.Y}.AddTo(canvas);
                new Line {X1 = 0, X2 = selection.TopLeft.X, Y1 = selection.TopLeft.Y, Y2 = selection.TopLeft.Y}.AddTo(canvas);
                new Line {X1 = selection.TopRight.X, X2 = destinationRect.Right, Y1 = selection.TopRight.Y, Y2 = selection.TopRight.Y}.AddTo(canvas);

                new Line {X1 = 0, X2 = selection.BottomLeft.X, Y1 = selection.BottomLeft.Y, Y2 = selection.BottomLeft.Y}.AddTo(canvas);
                new Line {X1 = selection.BottomRight.X, X2 = destinationRect.Right, Y1 = selection.BottomRight.Y, Y2 = selection.BottomRight.Y}.AddTo(canvas);
                new Line {X1 = selection.BottomLeft.X, X2 = selection.BottomLeft.X, Y1 = selection.BottomLeft.Y, Y2 = destinationRect.Bottom}.AddTo(canvas);
                new Line {X1 = selection.BottomRight.X, X2 = selection.BottomRight.X, Y1 = selection.BottomRight.Y, Y2 = destinationRect.Bottom}.AddTo(canvas);

                var selectionRect = new Rectangle
                {
                    Width = selection.Width,
                    Height = selection.Height,
                    StrokeThickness = 1,
                    Stroke = Stroke
                }.AddTo(canvas);

                Canvas.SetLeft(selectionRect, selection.Left);
                Canvas.SetTop(selectionRect, selection.Top);
            }
            else
            {
                new Line {X1 = 0, X2 = destinationRect.Width, Y1 = mousePosition.Y, Y2 = mousePosition.Y}.AddTo(canvas);
                new Line {X1 = mousePosition.X, X2 = mousePosition.X, Y1 = 0, Y2 = destinationRect.Height}.AddTo(canvas);
            }

            foreach (var line in canvas.Children.OfType<Line>())
            {
                line.SetCurrentValue(Shape.StrokeProperty, Stroke);
                line.SetCurrentValue(Shape.StrokeThicknessProperty, StrokeThickness);
                line.SetCurrentValue(Shape.StrokeDashArrayProperty, lineDashArray);
            }

            adornerLayer.InvalidateArrange();
        }

        protected override Visual GetVisualChild(int index)
        {
            return content;
        }
    }
}
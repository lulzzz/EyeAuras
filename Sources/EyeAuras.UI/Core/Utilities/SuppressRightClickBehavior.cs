using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;
using log4net;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Core.Utilities
{
    public class SuppressRightClickBehavior : Behavior<UIElement>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SuppressRightClickBehavior));

        private readonly SerialDisposable attachmentAnchor = new SerialDisposable();

        protected override void OnAttached()
        {
            base.OnAttached();

            var anchors = new CompositeDisposable();
            attachmentAnchor.Disposable = anchors;

            Observable
                .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
                    h => AssociatedObject.MouseRightButtonDown += h,
                    h => AssociatedObject.MouseRightButtonDown -= h)
                .Subscribe(
                    () =>
                    {
                        Log.Warn($"Fixing DragablzTabControl bug - restoring IsHitTestVisible to true for item {AssociatedObject}");
                        AssociatedObject.SetCurrentValue(UIElement.IsHitTestVisibleProperty, true);
                    })
                .AddTo(anchors);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            attachmentAnchor.Disposable = null;
        }
    }
}
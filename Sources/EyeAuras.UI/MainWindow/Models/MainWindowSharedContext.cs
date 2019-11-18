using EyeAuras.Shared;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.MainWindow.Models
{
    public sealed class MainWindowSharedContext : DisposableReactiveObject, ISharedContext
    {
        public IComplexAuraTrigger SystemTrigger { get; }

        public MainWindowSharedContext()
        {
            SystemTrigger = new ComplexAuraTrigger().AddTo(Anchors);
        }
    }
}
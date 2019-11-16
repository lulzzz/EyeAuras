using PoeShared.Scaffolding;

namespace EyeAuras.UI.MainWindow.ViewModels
{
    internal interface IMessageBoxViewModel : IDisposableReactiveObject
    {
        string Content { get; set; }

        string Title { get; set; }

        string ContentHint { get; set; }

        bool IsOpen { get; set; }
    }
}
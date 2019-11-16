using JetBrains.Annotations;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;

namespace EyeAuras.UI.MainWindow.ViewModels
{
    internal sealed class MessageBoxViewModel : DisposableReactiveObject, IMessageBoxViewModel
    {
        private string content;
        private string contentHint;
        private bool isOpen;
        private string title;

        public MessageBoxViewModel(
            [NotNull] IClipboardManager clipboardManager
        )
        {
            CloseMessageBoxCommand = CommandWrapper.Create(() => IsOpen = false);
            CopyAllCommand = CommandWrapper.Create(() => clipboardManager.SetText(Content));
        }

        public CommandWrapper CloseMessageBoxCommand { get; }

        public CommandWrapper CopyAllCommand { get; }

        public string ContentHint
        {
            get => contentHint;
            set => RaiseAndSetIfChanged(ref contentHint, value);
        }

        public string Content
        {
            get => content;
            set => RaiseAndSetIfChanged(ref content, value);
        }

        public string Title
        {
            get => title;
            set => RaiseAndSetIfChanged(ref title, value);
        }

        public bool IsOpen
        {
            get => isOpen;
            set => RaiseAndSetIfChanged(ref isOpen, value);
        }
    }
}
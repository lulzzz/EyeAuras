using System.Reactive.Disposables;
using System.Windows.Input;
using EyeAuras.Shared;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class PropertyEditorViewModel : DisposableReactiveObject, IPropertyEditorViewModel
    {
        private readonly SerialDisposable activeValueEditorAnchors = new SerialDisposable();
        private readonly IAuraRepository auraRepository;
        private readonly CommandWrapper closeCommand;
        private ICloseController closeController;
        private IAuraModel value;
        private IAuraPropertiesEditor valueEditor;

        public PropertyEditorViewModel(
            [NotNull] IAuraRepository auraRepository)
        {
            this.auraRepository = auraRepository;
            activeValueEditorAnchors.AddTo(Anchors);

            closeCommand = CommandWrapper.Create(CloseCommandExecuted, CloseCommandCanExecute);

            this.WhenAnyValue(x => x.Value)
                .Subscribe(Reinitialize)
                .AddTo(Anchors);
        }

        public IAuraPropertiesEditor ValueEditor
        {
            get => valueEditor;
            private set => RaiseAndSetIfChanged(ref valueEditor, value);
        }

        public ICommand CloseCommand => closeCommand;

        public void SetCloseController(ICloseController closeController)
        {
            this.closeController = closeController;
            closeCommand.RaiseCanExecuteChanged();
        }

        public IAuraModel Value
        {
            get => value;
            set => RaiseAndSetIfChanged(ref this.value, value);
        }

        private bool CloseCommandCanExecute()
        {
            return closeController != null;
        }

        private void CloseCommandExecuted()
        {
            Guard.ArgumentIsTrue(() => CloseCommandCanExecute());
            closeController.Close();
        }

        private void Reinitialize()
        {
            var editorAnchors = new CompositeDisposable();
            activeValueEditorAnchors.Disposable = editorAnchors;

            if (value == null)
            {
                ValueEditor = null;
                return;
            }

            ValueEditor = auraRepository.CreateEditor(value);
            if (ValueEditor != null)
            {
                ValueEditor.AddTo(editorAnchors);
                ValueEditor.Source = value;
            }
        }

        public override string ToString()
        {
            return $"{nameof(PropertyEditorViewModel)} {nameof(Value)}: {Value}, {nameof(ValueEditor)}: {ValueEditor}";
        }
    }
}
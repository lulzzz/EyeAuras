using System.Windows.Input;
using EyeAuras.Shared;
using JetBrains.Annotations;
using PoeShared.Native;

namespace EyeAuras.UI.Core.ViewModels
{
    internal interface IPropertyEditorViewModel
    {
        ICommand CloseCommand { [NotNull] get; }

        IAuraModel Value { [CanBeNull] get; [CanBeNull] set; }

        void SetCloseController([NotNull] ICloseController closeController);
    }
}
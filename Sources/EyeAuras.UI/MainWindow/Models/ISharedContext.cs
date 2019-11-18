using EyeAuras.Shared;
using JetBrains.Annotations;

namespace EyeAuras.UI.MainWindow.Models
{
    internal interface ISharedContext
    {
        IComplexAuraTrigger SystemTrigger { [NotNull] get; }
    }
}
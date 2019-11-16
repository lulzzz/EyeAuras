using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace EyeAuras.UI.MainWindow.Models
{
    public interface IMainWindowBlocksProvider
    {
        ReadOnlyObservableCollection<object> StatusBarItems { [NotNull] get; }
    }
}
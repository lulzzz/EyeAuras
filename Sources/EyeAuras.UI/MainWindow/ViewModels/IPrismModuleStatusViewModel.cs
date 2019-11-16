using System.Collections.ObjectModel;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.MainWindow.ViewModels
{
    internal interface IPrismModuleStatusViewModel : IDisposableReactiveObject
    {
        bool IsVisible { get; }
        
        ReadOnlyObservableCollection<PrismModuleStatus> Modules { [NotNull] get; }
    }
}
using System.Collections.ObjectModel;
using EyeAuras.OnTopReplica;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace EyeAuras.Shared.Services
{
    public interface IWindowListProvider : IDisposableReactiveObject
    {
        ReadOnlyObservableCollection<WindowHandle> WindowList { [NotNull] get; }
    }
}
using EyeAuras.OnTopReplica;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace EyeAuras.Shared.Services
{
    public interface IWindowSelectorViewModel : IDisposableReactiveObject
    {
        WindowHandle ActiveWindow { [CanBeNull] get; [CanBeNull] set; }
        
        WindowHandle[] MatchingWindowList { [NotNull] get; }
        
        WindowMatchParams TargetWindow { get; set; }
    }
}
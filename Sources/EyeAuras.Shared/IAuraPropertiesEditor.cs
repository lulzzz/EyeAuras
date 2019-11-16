using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace EyeAuras.Shared
{
    public interface IAuraPropertiesEditor<in T> : IAuraPropertiesEditor where T : IAuraModel
    {
    }

    public interface IAuraPropertiesEditor : IDisposableReactiveObject
    {
        IAuraModel Source { [CanBeNull] get; [CanBeNull] set; }
    }
}
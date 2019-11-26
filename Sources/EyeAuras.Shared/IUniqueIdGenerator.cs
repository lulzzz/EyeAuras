using JetBrains.Annotations;

namespace EyeAuras.Shared
{
    public interface IUniqueIdGenerator
    {
        [NotNull]
        string Next();
    }
}
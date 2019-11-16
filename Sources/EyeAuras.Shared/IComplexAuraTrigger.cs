using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace EyeAuras.Shared
{
    public interface IComplexAuraTrigger : IAuraTrigger
    {
        ObservableCollection<IAuraTrigger> Triggers { [NotNull] get; }
    }
}
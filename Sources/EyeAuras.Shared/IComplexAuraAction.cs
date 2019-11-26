using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace EyeAuras.Shared
{
    public interface IComplexAuraAction : IAuraAction
    {
        ObservableCollection<IAuraAction> Actions { [NotNull] get; }
    }
}
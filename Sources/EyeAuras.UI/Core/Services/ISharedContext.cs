using System.Collections.ObjectModel;
using EyeAuras.Shared;
using EyeAuras.UI.Core.ViewModels;
using JetBrains.Annotations;

namespace EyeAuras.UI.Core.Services
{
    internal interface ISharedContext
    {
        IComplexAuraTrigger SystemTrigger { [NotNull] get; }
        
        ObservableCollection<IEyeAuraViewModel> AuraList { [NotNull] get; }
    }
}
using System.Collections.ObjectModel;
using DynamicData.Annotations;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Overlay.ViewModels;
using PoeShared.Native;

namespace EyeAuras.UI.Core.Models
{
    internal interface IOverlayAuraModel : IAuraModel<OverlayAuraProperties>, IAuraModelController
    {
        bool IsActive { get; }

        WindowMatchParams TargetWindow { [CanBeNull] get; [CanBeNull] set; }
        
        ObservableCollection<IAuraTrigger> Triggers { [NotNull] get; }
        
        ObservableCollection<IAuraAction> OnEnterActions { get; }

        IEyeOverlayViewModel Overlay { [NotNull] get; }
        
        void SetCloseController([NotNull] ICloseController closeController);
    }
}
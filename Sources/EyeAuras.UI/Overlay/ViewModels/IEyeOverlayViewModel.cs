using System.Windows.Input;
using System.Windows.Media;
using DynamicData.Annotations;
using EyeAuras.OnTopReplica;
using PoeShared.Native;

namespace EyeAuras.UI.Overlay.ViewModels
{
    internal interface IEyeOverlayViewModel : IOverlayViewModel
    {
        WindowHandle AttachedWindow { get; set; }

        ThumbnailRegion Region { [NotNull] get; }

        ICommand ResetRegionCommand { [NotNull] get; }
        
        Color BorderColor { get; set; }
        
        double BorderThickness { get; set; }

        string OverlayName { get; }

        bool IsClickThrough { get; set; }

        double ThumbnailOpacity { get; set; }
        
        bool MaintainAspectRatio { get; set; }

        void ScaleOverlay(double scaleRatio);
    }
}
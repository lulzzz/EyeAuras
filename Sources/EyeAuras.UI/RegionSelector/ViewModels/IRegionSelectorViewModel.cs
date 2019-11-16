using System.Drawing;
using EyeAuras.UI.RegionSelector.Services;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.RegionSelector.ViewModels
{
    public interface IRegionSelectorViewModel : IDisposableReactiveObject
    {
        Rectangle Selection { get; set; }
        
        RegionSelectorResult MouseRegion { [CanBeNull] get; }
        
        Point MouseLocation { get; }
    }
}
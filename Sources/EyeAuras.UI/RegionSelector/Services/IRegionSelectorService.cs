using System;
using JetBrains.Annotations;

namespace EyeAuras.UI.RegionSelector.Services
{
    internal interface IRegionSelectorService
    {
        [NotNull] 
        IObservable<RegionSelectorResult> SelectRegion();
    }
}
using System;
using EyeAuras.UI.RegionSelector.Services;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.RegionSelector.ViewModels
{
    public interface IRegionSelectorViewModel : IDisposableReactiveObject
    {
        RegionSelectorResult SelectionCandidate { [CanBeNull] get; }
        
        ISelectionAdornerViewModel SelectionAdorner { [NotNull] get; }

        [NotNull]
        IObservable<RegionSelectorResult> SelectWindow();
    }
}
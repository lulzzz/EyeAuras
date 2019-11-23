using System;
using System.Windows;
using System.Windows.Media;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.RegionSelector.ViewModels
{
    public interface ISelectionAdornerViewModel : IDisposableReactiveObject
    {
        double StrokeThickness { get; }
        
        Brush Stroke { [CanBeNull] get; }
        
        Point AnchorPoint { get; }
        
        Point MousePosition { get; }
        
        UIElement Owner { [CanBeNull] get; }
        
        [NotNull]
        IObservable<Rect> StartSelection();
    }
}
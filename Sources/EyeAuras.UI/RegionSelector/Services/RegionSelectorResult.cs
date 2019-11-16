using System.Drawing;
using EyeAuras.OnTopReplica;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.RegionSelector.Services
{
    public sealed class RegionSelectorResult
    {
        public WindowHandle Window { get; set; }
        
        public Rectangle Selection { get; set; }
        
        public Rectangle AbsoluteSelection { get; set; }
        
        public string Reason { get; set; }

        public bool IsValid => GeometryExtensions.IsNotEmpty(Selection) && Window != null;

        public override string ToString()
        {
            return $"{nameof(Window)}: {Window}, {nameof(Selection)}: {Selection}, {nameof(Reason)}: {Reason}";
        }
    }
}
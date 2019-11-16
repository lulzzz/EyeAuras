using System.Windows.Media.Imaging;
using EyeAuras.OnTopReplica;

namespace EyeAuras.UI.Core.Models
{
    internal struct WindowListItem
    {
        public bool IsMatching { get; set; }

        public WindowHandle Window { get; set; }

        public string Title => Window?.Title;

        public BitmapSource Icon => Window?.IconBitmap;
    }
}
using System;
using System.Drawing;
using System.Windows.Input;

namespace EyeAuras.OnTopReplica
{
    /// <summary>
    ///     EventArgs structure for clicks on a cloned window.
    /// </summary>
    public class CloneClickEventArgs : EventArgs
    {
        public CloneClickEventArgs(Point location, MouseButton buttons)
        {
            ClientClickLocation = location;
            Buttons = buttons;
            IsDoubleClick = false;
        }

        public CloneClickEventArgs(Point location, MouseButton buttons, bool doubleClick)
        {
            ClientClickLocation = location;
            Buttons = buttons;
            IsDoubleClick = doubleClick;
        }

        public Point ClientClickLocation { get; set; }

        public bool IsDoubleClick { get; set; }

        public MouseButton Buttons { get; set; }
    }
}
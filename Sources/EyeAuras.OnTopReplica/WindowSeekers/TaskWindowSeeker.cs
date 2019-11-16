using System.Collections.Generic;
using EyeAuras.OnTopReplica.Native;

namespace EyeAuras.OnTopReplica.WindowSeekers
{
    /// <summary>
    ///     Window seeker that attempts to mimic ALT+TAB behavior in filtering windows to show.
    /// </summary>
    public sealed class TaskWindowSeeker : BaseWindowSeeker
    {
        public override IList<WindowHandle> Windows { get; } = new List<WindowHandle>();

        public override void Refresh()
        {
            Windows.Clear();
            base.Refresh();
        }

        protected override bool InspectWindow(WindowHandle handle)
        {
            //Code taken from: http://www.thescarms.com/VBasic/alttab.aspx

            //Reject empty titles
            if (string.IsNullOrEmpty(handle.Title))
            {
                return true;
            }

            //Accept windows that
            // - are visible
            // - do not have a parent
            // - have no owner and are not Tool windows OR
            // - have an owner and are App windows
            if ((long) WindowManagerMethods.GetParent(handle.Handle) != 0)
            {
                return true;
            }

            var hasOwner = (long) WindowManagerMethods.GetWindow(handle.Handle, WindowManagerMethods.GetWindowMode.GwOwner) != 0;
            var exStyle = (WindowMethods.WindowExStyles) WindowMethods.GetWindowLong(handle.Handle, WindowMethods.WindowLong.ExStyle);

            if ((exStyle & WindowMethods.WindowExStyles.ToolWindow) == 0 && !hasOwner || //unowned non-tool window
                (exStyle & WindowMethods.WindowExStyles.AppWindow) == WindowMethods.WindowExStyles.AppWindow && hasOwner)
            {
                //owned application window
                Windows.Add(handle);
            }

            return true;
        }
    }
}
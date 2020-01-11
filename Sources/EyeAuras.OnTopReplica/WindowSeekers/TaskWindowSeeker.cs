using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EyeAuras.OnTopReplica.Native;
using PoeShared.Native;

namespace EyeAuras.OnTopReplica.WindowSeekers
{
    /// <summary>
    ///     Window seeker that attempts to mimic ALT+TAB behavior in filtering windows to show.
    /// </summary>
    public sealed class TaskWindowSeeker : BaseWindowSeeker
    {
        public override IReadOnlyCollection<WindowHandle> Windows { get; protected set; } = new List<WindowHandle>();

        public override void Refresh()
        {
            var windowsSnapshot = new List<WindowHandle>();
            WindowManagerMethods.EnumWindows((hwnd, lParam) => RefreshCallback(
                hwnd,
                handle =>
                {
                    handle.ZOrder = windowsSnapshot.Count;
                    windowsSnapshot.Add(handle);
                }), 
                IntPtr.Zero);
            
            Windows = new ReadOnlyCollection<WindowHandle>(windowsSnapshot);
        }

        private bool RefreshCallback(IntPtr hwnd, Action<WindowHandle> addHandler)
        {
            if (BlacklistedWindows.Contains(hwnd))
            {
                return true;
            }

            if (SkipNotVisibleWindows && !WindowManagerMethods.IsWindowVisible(hwnd))
            {
                return true;
            }

            var handle = new WindowHandle(hwnd);

            return InspectWindow(handle, addHandler);
        }

        private bool InspectWindow(WindowHandle handle, Action<WindowHandle> addHandler)
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
                addHandler(handle);
            }

            return true;
        }
        
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using EyeAuras.OnTopReplica.Native;
using PoeShared.Native;

namespace EyeAuras.OnTopReplica.WindowSeekers
{
    /// <summary>
    ///     Base class for window seekers that can populate a list of window handles based on some criteria and with basic
    ///     filtering.
    /// </summary>
    public abstract class BaseWindowSeeker : IWindowSeeker
    {
        protected BaseWindowSeeker()
        {
            BlacklistedWindows = new HashSet<IntPtr>();
        }

        /// <summary>
        ///     Gets or sets the window handle of the owner.
        /// </summary>
        /// <remarks>
        ///     Windows with this handle will be automatically skipped.
        /// </remarks>
        public ISet<IntPtr> BlacklistedWindows { get; }

        /// <summary>
        ///     Gets or sets whether not visible windows should be skipped.
        /// </summary>
        public bool SkipNotVisibleWindows { get; set; }

        private bool RefreshCallback(IntPtr hwnd, IntPtr lParam)
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

            return InspectWindow(handle);
        }

        /// <summary>
        ///     Inspects a window and return whether inspection should continue.
        /// </summary>
        /// <param name="handle">Handle of the window.</param>
        /// <returns>True if inspection should continue. False stops current refresh operation.</returns>
        protected abstract bool InspectWindow(WindowHandle handle);

        /// <summary>
        ///     Get the matching windows from the last refresh.
        /// </summary>
        public abstract IList<WindowHandle> Windows { get; }

        /// <summary>
        ///     Forces a window list refresh.
        /// </summary>
        public virtual void Refresh()
        {
            WindowManagerMethods.EnumWindows(RefreshCallback, IntPtr.Zero);
            var windows = Windows.ToArray();
            var zOrder = UnsafeNative.GetZOrder(windows.Select(x => x.Handle).ToArray());
            for (var i = 0; i < zOrder.Length; i++)
            {
                windows[i].ZOrder = zOrder[i];
            }
        }
    }
}
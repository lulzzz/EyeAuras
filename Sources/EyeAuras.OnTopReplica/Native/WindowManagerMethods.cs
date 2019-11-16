using System;
using System.Runtime.InteropServices;

namespace EyeAuras.OnTopReplica.Native
{
    /// <summary>
    ///     Common Win32 Window Manager native methods.
    /// </summary>
    internal static class WindowManagerMethods
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public enum GetWindowMode : uint
        {
            GwOwner = 4,
        }

        [DllImport("user32.dll")]
        public static extern IntPtr RealChildWindowFromPoint(IntPtr parent, NPoint point);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hwnd, ref NPoint point);

        /// <summary>
        ///     Converts a point in client coordinates of a window to screen coordinates.
        /// </summary>
        /// <param name="hwnd">Handle to the window of the original point.</param>
        /// <param name="clientPoint">Point expressed in client coordinates.</param>
        /// <returns>Point expressed in screen coordinates.</returns>
        public static NPoint ClientToScreen(IntPtr hwnd, NPoint clientPoint)
        {
            var localCopy = new NPoint(clientPoint);

            if (ClientToScreen(hwnd, ref localCopy))
            {
                return localCopy;
            }

            return new NPoint();
        }

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hwnd, ref NPoint point);

        /// <summary>
        ///     Converts a point in screen coordinates in client coordinates relative to a window.
        /// </summary>
        /// <param name="hwnd">Handle of the window whose client coordinate system should be used.</param>
        /// <param name="screenPoint">Point expressed in screen coordinates.</param>
        /// <returns>Point expressed in client coordinates.</returns>
        public static NPoint ScreenToClient(IntPtr hwnd, NPoint screenPoint)
        {
            var localCopy = new NPoint(screenPoint);

            if (ScreenToClient(hwnd, ref localCopy))
            {
                return localCopy;
            }

            return new NPoint();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("User32", CharSet = CharSet.Auto)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndParent);

        [DllImport("user32.dll", SetLastError = false)]
        public static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hwnd, GetWindowMode mode);

        /// <summary>
        ///     Checks whether a window is a top-level window (has no owner nor parent window).
        /// </summary>
        /// <param name="hwnd">Handle to the window to check.</param>
        public static bool IsTopLevel(IntPtr hwnd)
        {
            var hasParent = GetParent(hwnd).ToInt64() != 0;
            var hasOwner = GetWindow(hwnd, GetWindowMode.GwOwner).ToInt64() != 0;

            return !hasParent && !hasOwner;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string className, string windowName);
    }
}
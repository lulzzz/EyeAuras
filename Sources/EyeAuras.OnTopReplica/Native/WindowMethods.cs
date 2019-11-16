using System;
using System.Runtime.InteropServices;
using System.Text;

namespace EyeAuras.OnTopReplica.Native
{
    /// <summary>
    ///     Common Win32 methods for operating on windows.
    /// </summary>
    internal static class WindowMethods
    {
        [Flags]
        public enum WindowExStyles : long
        {
            AppWindow = 0x40000,
            ToolWindow = 0x80,
        }

        public enum WindowLong
        {
            ExStyle = -20,
        }

        public static IntPtr GetWindowLong(IntPtr hWnd, WindowLong i)
        {
            if (IntPtr.Size == 8)
            {
                return GetWindowLongPtr64(hWnd, i);
            }

            return new IntPtr(GetWindowLong32(hWnd, i));
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, WindowLong nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, WindowLong nIndex);

        [DllImport("user32.dll")]
        public static extern IntPtr GetMenu(IntPtr hwnd);

        public enum ClassLong
        {
            Icon = -14,
            IconSmall = -34
        }

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW")]
        private static extern IntPtr GetClassLong64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetClassLongW")]
        private static extern int GetClassLong32(IntPtr hWnd, int nIndex);

        public static IntPtr GetClassLong(IntPtr hWnd, ClassLong i)
        {
            if (IntPtr.Size == 8)
            {
                return GetClassLong64(hWnd, (int) i);
            }

            return new IntPtr(GetClassLong32(hWnd, (int) i));
        }
    }
}
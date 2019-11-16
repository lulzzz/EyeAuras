using System;
using System.Runtime.InteropServices;

namespace EyeAuras.OnTopReplica.Native
{
    /// <summary>
    ///     Common methods for Win32 messaging.
    /// </summary>
    internal static class MessagingMethods
    {
        [Flags]
        public enum SendMessageTimeoutFlags : uint
        {
            AbortIfHung = 2,
            Block = 1,
            Normal = 0
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam, SendMessageTimeoutFlags flags, uint timeout,
            out IntPtr result);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = false)]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public static IntPtr MakeLParam(int loWord, int hiWord)
        {
            return new IntPtr((hiWord << 16) | (loWord & 0xffff));
        }
    }
}
using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using EyeAuras.OnTopReplica.Native;
using log4net;
using Newtonsoft.Json;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace EyeAuras.OnTopReplica
{
    public sealed class WindowHandle : IWin32Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowHandle));

        public WindowHandle(IntPtr handle)
        {
            //FIXME Add Light version of WindowHandle which will be mostly Lazy<> 
            Handle = handle;
            Title = UnsafeNative.GetWindowTitle(handle);
            Icon = GetWindowIcon(handle);
            Class = UnsafeNative.GetWindowClass(handle);
            WindowBounds = UnsafeNative.GetWindowRect(handle);
            ClientBounds = UnsafeNative.GetClientRect(handle);
            try
            {
                IconBitmap = Icon != null
                    ? Imaging.CreateBitmapSourceFromHIcon(Icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
                    : null;
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to get IconBitmap, window: {Title}, class: {Class}", ex);
            }
            IconBitmap?.Freeze();
            ProcessId = UnsafeNative.GetProcessIdByWindowHandle(handle);
        }
        
        public string Title { get; }

        public Rectangle WindowBounds { get; }
        
        public Rectangle ClientBounds { get; }

        [JsonIgnore]
        public Icon Icon { get; }

        [JsonIgnore]
        public BitmapSource IconBitmap { get; }

        public string Class { get; }

        public IntPtr Handle { get; }

        public int ProcessId { get; }
        
        public int ZOrder { get; set; }

        private static Icon GetWindowIcon(IntPtr handle)
        {
            if (MessagingMethods.SendMessageTimeout(
                    handle,
                    Wm.Geticon,
                    new IntPtr(0),
                    new IntPtr(0),
                    MessagingMethods.SendMessageTimeoutFlags.AbortIfHung | MessagingMethods.SendMessageTimeoutFlags.Block,
                    500,
                    out var hIcon) ==
                IntPtr.Zero)
            {
                hIcon = IntPtr.Zero;
            }

            Icon result = null;
            if (hIcon != IntPtr.Zero)
            {
                result = Icon.FromHandle(hIcon);
            }
            else
            {
                //Fetch icon from window class
                hIcon = WindowMethods.GetClassLong(handle, WindowMethods.ClassLong.Icon);

                if (hIcon.ToInt64() != 0)
                {
                    result = Icon.FromHandle(hIcon);
                }
            }

            return result;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Handle.ToHexadecimal());

            if (!string.IsNullOrWhiteSpace(Title))
            {
                sb.Append($" (title: {Title})");
            }

            if (!string.IsNullOrWhiteSpace(Class))
            {
                sb.Append($" (class: {Class})");
            }

            return sb.ToString();
        }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(other, this))
            {
                return true;
            }

            var win = other as WindowHandle;
            if (win == null)
            {
                return false;
            }

            return Handle.Equals(win.Handle);
        }

        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }
    }
}
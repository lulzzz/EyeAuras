using System;
using System.Windows.Input;
using EyeAuras.OnTopReplica.Native;
using log4net;

namespace EyeAuras.OnTopReplica
{
    internal static class Win32Helper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Win32Helper));

        /// <summary>Returns the child control of a window corresponding to a screen location.</summary>
        /// <param name="parent">Parent window to explore.</param>
        /// <param name="scrClickLocation">Child control location in screen coordinates.</param>
        private static IntPtr GetRealChildControlFromPoint(IntPtr parent, NPoint scrClickLocation)
        {
            IntPtr curr = parent, child = IntPtr.Zero;
            do
            {
                child = WindowManagerMethods.RealChildWindowFromPoint(
                    curr,
                    WindowManagerMethods.ScreenToClient(curr, scrClickLocation));

                if (child == IntPtr.Zero || child == curr)
                {
                    break;
                }

                //Update for next loop
                curr = child;
            } while (true);

            //Safety check, shouldn't happen
            if (curr == IntPtr.Zero)
            {
                curr = parent;
            }

            return curr;
        }

        #region Injection

        /// <summary>Inject a fake left mouse click on a target window, on a location expressed in client coordinates.</summary>
        /// <param name="window">Target window to click on.</param>
        /// <param name="clickArgs"></param>
        public static void InjectFakeMouseClick(IntPtr window, CloneClickEventArgs clickArgs)
        {
            var clientClickLocation = NPoint.FromPoint(clickArgs.ClientClickLocation);
            var scrClickLocation = WindowManagerMethods.ClientToScreen(window, clientClickLocation);

            //HACK (?)
            //If target window has a Menu (which appears on the thumbnail) move the clicked location down
            //in order to adjust (the menu isn't part of the window's client rect).
            var hMenu = WindowMethods.GetMenu(window);

            //if (hMenu != IntPtr.Zero)
            //	scrClickLocation.Y -= SystemInformation.MenuHeight;

            var hChild = GetRealChildControlFromPoint(window, scrClickLocation);
            var clntClickLocation = WindowManagerMethods.ScreenToClient(hChild, scrClickLocation);

            if (clickArgs.Buttons == MouseButton.Left)
            {
                if (clickArgs.IsDoubleClick)
                {
                    InjectDoubleLeftMouseClick(hChild, clntClickLocation);
                }
                else
                {
                    InjectLeftMouseClick(hChild, clntClickLocation);
                }
            }
            else if (clickArgs.Buttons == MouseButton.Right)
            {
                if (clickArgs.IsDoubleClick)
                {
                    InjectDoubleRightMouseClick(hChild, clntClickLocation);
                }
                else
                {
                    InjectRightMouseClick(hChild, clntClickLocation);
                }
            }
        }

        private static void InjectLeftMouseClick(IntPtr child, NPoint clientLocation)
        {
            var lParamClickLocation = MessagingMethods.MakeLParam(clientLocation.X, clientLocation.Y);

            MessagingMethods.PostMessage(child, Wm.Lbuttondown, new IntPtr(Mk.Lbutton), lParamClickLocation);
            MessagingMethods.PostMessage(child, Wm.Lbuttonup, new IntPtr(Mk.Lbutton), lParamClickLocation);

            Log.Debug($"Left click on window #{child} at {clientLocation}");
        }

        private static void InjectRightMouseClick(IntPtr child, NPoint clientLocation)
        {
            var lParamClickLocation = MessagingMethods.MakeLParam(clientLocation.X, clientLocation.Y);

            MessagingMethods.PostMessage(child, Wm.Rbuttondown, new IntPtr(Mk.Rbutton), lParamClickLocation);
            MessagingMethods.PostMessage(child, Wm.Rbuttonup, new IntPtr(Mk.Rbutton), lParamClickLocation);

            Log.Debug($"Right click on window #{child} at {clientLocation}");
        }

        private static void InjectDoubleLeftMouseClick(IntPtr child, NPoint clientLocation)
        {
            var lParamClickLocation = MessagingMethods.MakeLParam(clientLocation.X, clientLocation.Y);

            MessagingMethods.PostMessage(child, Wm.Lbuttondblclk, new IntPtr(Mk.Lbutton), lParamClickLocation);

            Log.Debug($"Double left click on window #{child} at {clientLocation}");
        }

        private static void InjectDoubleRightMouseClick(IntPtr child, NPoint clientLocation)
        {
            var lParamClickLocation = MessagingMethods.MakeLParam(clientLocation.X, clientLocation.Y);

            MessagingMethods.PostMessage(child, Wm.Rbuttondblclk, new IntPtr(Mk.Rbutton), lParamClickLocation);

            Log.Debug($"Double right click on window #{child} at {clientLocation}");
        }

        #endregion
    }
}
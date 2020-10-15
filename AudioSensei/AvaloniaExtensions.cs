using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Serilog;

namespace AudioSensei
{
    public static class AvaloniaExtensions
    {
        public static void SetForegroundWindow(this Window window)
        {
            if (window == null)
            {
                return;
            }

            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }
            window.Activate();
            window.Focus();

#if WINDOWS
            IntPtr? handleNullable = window.PlatformImpl?.Handle?.Handle;
            if (handleNullable == null)
            {
                return;
            }
            IntPtr handle = handleNullable.Value;

            if (!IsWindow(handle))
            {
                Log.Warning("Invalid window handle!");
                return;
            }

            if (IsIconic(handle) && !ShowWindowAsync(handle, 5))
            {
                Log.Warning("ShowWindowAsync failed!");
            }

            if (SetActiveWindow(handle) == IntPtr.Zero)
            {
                Log.Warning(new Win32Exception(), "SetActiveWindow failed!");
            }

            if (!SetForegroundWindow(handle))
            {
                Log.Warning("SetForegroundWindow failed!");
            }

            if (SetFocus(handle) == IntPtr.Zero)
            {
                Log.Warning(new Win32Exception(), "SetFocus failed!");
            }
#endif
        }

#if WINDOWS
        private const string User32 = "user32";

        [DllImport(User32)]
        private static extern bool IsWindow(IntPtr hwnd);

        [DllImport(User32)]
        private static extern bool IsIconic(IntPtr hwnd);

        [DllImport(User32)]
        private static extern bool ShowWindowAsync(IntPtr hwnd, int nCmdShow);

        [DllImport(User32, SetLastError = true)]
        private static extern IntPtr SetActiveWindow(IntPtr hwnd);

        [DllImport(User32)]
        private static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport(User32, SetLastError = true)]
        private static extern IntPtr SetFocus(IntPtr hwnd);
#endif
    }
}

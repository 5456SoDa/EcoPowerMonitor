using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace EcoPowerMonitor.Services
{
    public static class WindowThemeService
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_TRANSITIONS_FORCEDISABLED = 3;
        private const int DWMWCP_ROUND = 2;

        private const int GWL_STYLE = -16;
        private const long WS_MINIMIZEBOX = 0x00020000L;
        private const long WS_MAXIMIZEBOX = 0x00010000L;
        private const long WS_SYSMENU = 0x00080000L;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        /// <summary>
        /// Enables native Windows Desktop Window Manager (DWM) taskbar minimize, restore and maximize animations.
        /// </summary>
        public static void EnableWindowDwmAnimations(Window window)
        {
            try
            {
                var helper = new WindowInteropHelper(window);
                var hwnd = helper.Handle;
                if (hwnd == IntPtr.Zero) return;

                // Ensure DWM transitions are never disabled
                int forceDisableTransitions = 0;
                DwmSetWindowAttribute(hwnd, DWMWA_TRANSITIONS_FORCEDISABLED, ref forceDisableTransitions, sizeof(int));

                // Add WS_MINIMIZEBOX, WS_MAXIMIZEBOX and WS_SYSMENU so DWM recognizes the window as animatable
                if (IntPtr.Size == 8)
                {
                    long style = GetWindowLongPtr(hwnd, GWL_STYLE).ToInt64();
                    style |= (WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);
                    SetWindowLongPtr(hwnd, GWL_STYLE, new IntPtr(style));
                }
                else
                {
                    int style = GetWindowLong32(hwnd, GWL_STYLE);
                    style |= (int)(WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);
                    SetWindowLong32(hwnd, GWL_STYLE, style);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to enable DWM window animations: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies native Windows 10 & 11 Dark or Light theme to the window titlebar and enables rounded corners.
        /// </summary>
        public static void ApplyTitlebarTheme(Window window, bool isDark)
        {
            try
            {
                var helper = new WindowInteropHelper(window);
                var hwnd = helper.Handle;
                if (hwnd == IntPtr.Zero) return;

                int darkMode = isDark ? 1 : 0;

                // Try modern Windows 10 build 19041+ and Windows 11 attribute (20)
                int res = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
                if (res != 0)
                {
                    // Fallback to Windows 10 1809/1903/1909 attribute (19)
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkMode, sizeof(int));
                }

                // Apply Windows 11 rounded corner preference
                int cornerPreference = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set DWM window titlebar theme: {ex.Message}");
            }
        }
    }
}

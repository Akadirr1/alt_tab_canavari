using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using MonitorAltTab.Models;

namespace MonitorAltTab.Core
{
    /// <summary>
    /// Enumerates all top-level windows and filters them to only include
    /// legitimate application windows suitable for Alt+Tab switching.
    /// </summary>
    public class WindowEnumerator
    {
        private IntPtr _overlayHwnd = IntPtr.Zero;
        private readonly IntPtr _shellWindow;

        // Class names to exclude
        private static readonly HashSet<string> ExcludedClassNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Progman",
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd",
            "DV2ControlHost",
            "MsgrIMEWindowClass",
            "SysShadow",
            "Button",
            "Windows.UI.Core.CoreWindow", // UWP overlay windows
            "Shell_InputSwitchTopLevelWindow",
            "SHELLDLL_DefView",
            "WorkerW",
        };

        public WindowEnumerator()
        {
            _shellWindow = NativeMethods.GetShellWindow();
        }

        /// <summary>
        /// Sets the overlay window handle so it can be excluded from enumeration.
        /// </summary>
        public void SetOverlayHandle(IntPtr hwnd)
        {
            _overlayHwnd = hwnd;
        }

        /// <summary>
        /// Enumerates all suitable Alt+Tab windows on the specified monitor.
        /// </summary>
        public List<WindowInfo> GetWindowsOnMonitor(IntPtr targetMonitor)
        {
            var results = new List<WindowInfo>();

            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (IsAltTabWindow(hWnd))
                {
                    // Check which monitor this window is on
                    IntPtr windowMonitor = NativeMethods.MonitorFromWindow(hWnd, NativeMethods.MONITOR_DEFAULTTONEAREST);

                    if (windowMonitor == targetMonitor)
                    {
                        var info = CreateWindowInfo(hWnd, windowMonitor);
                        if (info != null)
                        {
                            results.Add(info);
                        }
                    }
                }
                return true; // Continue enumeration
            }, IntPtr.Zero);

            Debug.WriteLine($"[WindowEnumerator] Found {results.Count} windows on monitor 0x{targetMonitor:X}");
            foreach (var w in results)
            {
                Debug.WriteLine($"  → {w.Title} (0x{w.Handle:X})");
            }

            return results;
        }

        /// <summary>
        /// Determines whether a window is suitable for Alt+Tab switching.
        /// Mirrors the logic Windows itself uses for its Alt+Tab list.
        /// </summary>
        private bool IsAltTabWindow(IntPtr hWnd)
        {
            // Skip our own overlay
            if (hWnd == _overlayHwnd)
                return false;

            // Skip shell window
            if (hWnd == _shellWindow)
                return false;

            // Must be visible
            if (!NativeMethods.IsWindowVisible(hWnd))
                return false;

            // Get window text — must have a title
            int textLength = NativeMethods.GetWindowTextLength(hWnd);
            if (textLength == 0)
                return false;

            // Get styles
            long exStyle = GetWindowLongSafe(hWnd, NativeMethods.GWL_EXSTYLE);
            long style = GetWindowLongSafe(hWnd, NativeMethods.GWL_STYLE);

            // Skip child windows
            if ((style & NativeMethods.WS_CHILD) != 0)
                return false;

            // Skip disabled windows
            if ((style & NativeMethods.WS_DISABLED) != 0)
                return false;

            // Check for cloaked windows (Windows 10/11 virtual desktops, UWP hidden)
            if (IsCloaked(hWnd))
                return false;

            // Get class name and check exclusion list
            var className = GetClassName(hWnd);
            if (ExcludedClassNames.Contains(className))
                return false;

            // ApplicationFrameWindow needs special handling for UWP
            if (className == "ApplicationFrameWindow")
            {
                // Only include if it has a visible child (the actual UWP content)
                if (IsCloaked(hWnd))
                    return false;
                // Let it pass through — the cloaked check above handles hidden UWP apps
            }

            // The Alt+Tab rule:
            // A window is shown in Alt+Tab if:
            //   - It has WS_EX_APPWINDOW, OR
            //   - It has no owner AND is not a tool window
            // A window is NOT shown if:
            //   - It has WS_EX_TOOLWINDOW (and not WS_EX_APPWINDOW)
            //   - It has an owner (and not WS_EX_APPWINDOW)

            bool isAppWindow = (exStyle & NativeMethods.WS_EX_APPWINDOW) != 0;
            bool isToolWindow = (exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0;
            bool isNoActivate = (exStyle & NativeMethods.WS_EX_NOACTIVATE) != 0;

            IntPtr owner = NativeMethods.GetWindow(hWnd, NativeMethods.GW_OWNER);
            bool hasOwner = owner != IntPtr.Zero;

            if (isAppWindow)
            {
                // WS_EX_APPWINDOW forces it into Alt+Tab regardless
                return true;
            }

            if (isToolWindow)
            {
                // Tool windows are excluded unless they also have APPWINDOW (handled above)
                return false;
            }

            if (isNoActivate)
            {
                return false;
            }

            if (hasOwner)
            {
                // Owned windows without APPWINDOW are not in Alt+Tab
                return false;
            }

            // Unowned, non-tool window without NOACTIVATE → include
            return true;
        }

        /// <summary>
        /// Check if a window is cloaked (hidden by Windows, e.g., on another virtual desktop).
        /// </summary>
        private static bool IsCloaked(IntPtr hWnd)
        {
            int cloaked = 0;
            int hr = NativeMethods.DwmGetWindowAttribute(hWnd, NativeMethods.DWMWA_CLOAKED, out cloaked, sizeof(int));
            return hr == 0 && cloaked != 0;
        }

        /// <summary>
        /// Gets the class name of a window.
        /// </summary>
        private static string GetClassName(IntPtr hWnd)
        {
            var sb = new StringBuilder(256);
            NativeMethods.GetClassName(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        /// <summary>
        /// Safe wrapper for GetWindowLong/GetWindowLongPtr (handles both 32/64 bit).
        /// </summary>
        private static long GetWindowLongSafe(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
            {
                return NativeMethods.GetWindowLongPtr(hWnd, nIndex).ToInt64();
            }
            return NativeMethods.GetWindowLong(hWnd, nIndex);
        }

        /// <summary>
        /// Creates a WindowInfo from a valid HWND.
        /// </summary>
        private WindowInfo? CreateWindowInfo(IntPtr hWnd, IntPtr monitor)
        {
            // Get title
            int length = NativeMethods.GetWindowTextLength(hWnd);
            if (length == 0) return null;

            var sb = new StringBuilder(length + 1);
            NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
            string title = sb.ToString();

            if (string.IsNullOrWhiteSpace(title))
                return null;

            // Get process name (best effort)
            string processName = "";
            try
            {
                NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
                using var process = Process.GetProcessById((int)pid);
                processName = process.ProcessName;
            }
            catch
            {
                // Ignore — some system processes may not be accessible
            }

            // Get icon
            BitmapSource? icon = GetWindowIcon(hWnd);

            return new WindowInfo
            {
                Handle = hWnd,
                Title = title,
                Icon = icon,
                MonitorHandle = monitor,
                IsMinimized = NativeMethods.IsIconic(hWnd),
                ProcessName = processName,
            };
        }

        /// <summary>
        /// Extracts the window icon using various fallback methods.
        /// </summary>
        private static BitmapSource? GetWindowIcon(IntPtr hWnd)
        {
            try
            {
                IntPtr iconHandle = IntPtr.Zero;

                // Try WM_GETICON with ICON_BIG
                NativeMethods.SendMessageTimeout(hWnd, NativeMethods.WM_GETICON, (IntPtr)NativeMethods.ICON_BIG, IntPtr.Zero,
                    NativeMethods.SMTO_ABORTIFHUNG, 100, out iconHandle);

                // Try ICON_SMALL
                if (iconHandle == IntPtr.Zero)
                {
                    NativeMethods.SendMessageTimeout(hWnd, NativeMethods.WM_GETICON, (IntPtr)NativeMethods.ICON_SMALL, IntPtr.Zero,
                        NativeMethods.SMTO_ABORTIFHUNG, 100, out iconHandle);
                }

                // Try ICON_SMALL2
                if (iconHandle == IntPtr.Zero)
                {
                    NativeMethods.SendMessageTimeout(hWnd, NativeMethods.WM_GETICON, (IntPtr)NativeMethods.ICON_SMALL2, IntPtr.Zero,
                        NativeMethods.SMTO_ABORTIFHUNG, 100, out iconHandle);
                }

                // Fallback to class icon
                if (iconHandle == IntPtr.Zero)
                {
                    iconHandle = NativeMethods.GetClassLongPtr(hWnd, NativeMethods.GCLP_HICON);
                }
                if (iconHandle == IntPtr.Zero)
                {
                    iconHandle = NativeMethods.GetClassLongPtr(hWnd, NativeMethods.GCLP_HICONSM);
                }

                if (iconHandle != IntPtr.Zero)
                {
                    var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                        iconHandle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    bitmapSource.Freeze(); // Make it thread-safe
                    return bitmapSource;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowEnumerator] Icon extraction failed for 0x{hWnd:X}: {ex.Message}");
            }

            return null;
        }
    }
}

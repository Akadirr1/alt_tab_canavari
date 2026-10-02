using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MonitorAltTab.Models;

namespace MonitorAltTab.Core
{
    /// <summary>
    /// Manages monitor detection: determines which monitor the cursor is on
    /// and provides monitor information.
    /// </summary>
    public static class MonitorManager
    {
        /// <summary>
        /// Gets the monitor handle where the mouse cursor currently resides.
        /// </summary>
        public static IntPtr GetMonitorFromCursor()
        {
            NativeMethods.GetCursorPos(out POINT pt);
            return NativeMethods.MonitorFromPoint(pt, NativeMethods.MONITOR_DEFAULTTONEAREST);
        }

        /// <summary>
        /// Gets the monitor handle for a given window.
        /// </summary>
        public static IntPtr GetMonitorFromWindow(IntPtr hwnd)
        {
            return NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        }

        /// <summary>
        /// Gets detailed monitor information for a given monitor handle.
        /// </summary>
        public static MonitorInfoModel? GetMonitorInfo(IntPtr hMonitor)
        {
            if (hMonitor == IntPtr.Zero) return null;

            var mi = new MONITORINFOEX();
            mi.cbSize = Marshal.SizeOf<MONITORINFOEX>();

            if (!NativeMethods.GetMonitorInfo(hMonitor, ref mi))
                return null;

            return new MonitorInfoModel
            {
                Handle = hMonitor,
                MonitorRect = mi.rcMonitor,
                WorkArea = mi.rcWork,
                IsPrimary = (mi.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0,
                DeviceName = mi.szDevice
            };
        }

        /// <summary>
        /// Enumerates all connected monitors.
        /// </summary>
        public static List<MonitorInfoModel> GetAllMonitors()
        {
            var monitors = new List<MonitorInfoModel>();

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
                {
                    var info = GetMonitorInfo(hMonitor);
                    if (info != null)
                    {
                        monitors.Add(info);
                        Debug.WriteLine($"[MonitorManager] Found monitor: {info}");
                    }
                    return true;
                }, IntPtr.Zero);

            return monitors;
        }

        /// <summary>
        /// Gets the cursor position.
        /// </summary>
        public static POINT GetCursorPosition()
        {
            NativeMethods.GetCursorPos(out POINT pt);
            return pt;
        }
    }
}

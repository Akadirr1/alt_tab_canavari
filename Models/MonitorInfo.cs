using System;
using System.Runtime.InteropServices;

namespace MonitorAltTab.Models
{
    /// <summary>
    /// Represents information about a display monitor.
    /// </summary>
    public class MonitorInfoModel
    {
        /// <summary>Monitor handle (HMONITOR).</summary>
        public IntPtr Handle { get; set; }

        /// <summary>Full monitor rectangle in virtual screen coordinates.</summary>
        public RECT MonitorRect { get; set; }

        /// <summary>Work area rectangle (excludes taskbar) in virtual screen coordinates.</summary>
        public RECT WorkArea { get; set; }

        /// <summary>Whether this is the primary monitor.</summary>
        public bool IsPrimary { get; set; }

        /// <summary>Device name of the monitor.</summary>
        public string DeviceName { get; set; } = string.Empty;

        public override string ToString() => $"{DeviceName} ({MonitorRect.Width}x{MonitorRect.Height}) Primary={IsPrimary}";
    }

    /// <summary>
    /// Win32 RECT structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;

        public override string ToString() => $"({Left},{Top})-({Right},{Bottom}) [{Width}x{Height}]";
    }

    /// <summary>
    /// Win32 POINT structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Win32 MONITORINFOEX structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }
}

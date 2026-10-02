using System;
using System.Windows.Media.Imaging;

namespace MonitorAltTab.Models
{
    /// <summary>
    /// Represents information about a top-level window suitable for Alt+Tab switching.
    /// </summary>
    public class WindowInfo
    {
        /// <summary>Window handle (HWND).</summary>
        public IntPtr Handle { get; set; }

        /// <summary>Window title text.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>The icon of the window, extracted via Win32 API.</summary>
        public BitmapSource? Icon { get; set; }

        /// <summary>The monitor handle this window belongs to.</summary>
        public IntPtr MonitorHandle { get; set; }

        /// <summary>Whether the window is currently minimized.</summary>
        public bool IsMinimized { get; set; }

        /// <summary>Process name for debugging purposes.</summary>
        public string ProcessName { get; set; } = string.Empty;

        public override string ToString() => $"{Title} (0x{Handle:X})";
    }
}

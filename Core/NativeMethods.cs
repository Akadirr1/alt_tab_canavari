using System;
using System.Runtime.InteropServices;

namespace MonitorAltTab.Core
{
    /// <summary>
    /// Centralized Win32 native method declarations used across the application.
    /// </summary>
    internal static class NativeMethods
    {
        // ── Keyboard Hook ──────────────────────────────────────────────
        public const int WH_KEYBOARD_LL = 13;

        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;

        public const int VK_TAB = 0x09;
        public const int VK_LMENU = 0xA4;  // Left Alt
        public const int VK_RMENU = 0xA5;  // Right Alt
        public const int VK_MENU = 0x12;   // Alt (generic)
        public const int VK_LSHIFT = 0xA0;
        public const int VK_RSHIFT = 0xA1;
        public const int VK_SHIFT = 0x10;
        public const int VK_ESCAPE = 0x1B;

        public const uint LLKHF_ALTDOWN = 0x20;

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string? lpModuleName);

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // ── Window Enumeration ─────────────────────────────────────────
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out Models.RECT lpRect);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetShellWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;

        public const uint WS_VISIBLE = 0x10000000;
        public const uint WS_DISABLED = 0x08000000;
        public const uint WS_MINIMIZE = 0x20000000;
        public const uint WS_POPUP = 0x80000000;
        public const uint WS_CHILD = 0x40000000;
        public const long WS_EX_TOOLWINDOW = 0x00000080;
        public const long WS_EX_APPWINDOW = 0x00040000;
        public const long WS_EX_NOACTIVATE = 0x08000000L;

        public const uint GW_OWNER = 4;

        // ── DWM Cloaked ────────────────────────────────────────────────
        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        public const int DWMWA_CLOAKED = 14;

        // ── Monitor ────────────────────────────────────────────────────
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(Models.POINT pt, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref Models.MONITORINFOEX lpmi);

        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Models.RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        public const uint MONITOR_DEFAULTTONEAREST = 2;
        public const uint MONITOR_DEFAULTTONULL = 0;
        public const uint MONITOR_DEFAULTTOPRIMARY = 1;

        public const uint MONITORINFOF_PRIMARY = 1;

        // ── Cursor ─────────────────────────────────────────────────────
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out Models.POINT lpPoint);

        // ── Window Activation ──────────────────────────────────────────
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        public static extern bool AllowSetForegroundWindow(int dwProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        public const int SW_RESTORE = 9;
        public const int SW_SHOW = 5;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_MINIMIZE = 6;

        // ── Icon Extraction ────────────────────────────────────────────
        public const uint WM_GETICON = 0x007F;
        public const uint ICON_BIG = 1;
        public const uint ICON_SMALL = 0;
        public const uint ICON_SMALL2 = 2;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        public const uint SMTO_ABORTIFHUNG = 0x0002;

        [DllImport("user32.dll")]
        public static extern IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);

        public const int GCLP_HICON = -14;
        public const int GCLP_HICONSM = -34;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        // ── Input Simulation (for suppressing system Alt+Tab) ──────────
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        // ── System Metrics ─────────────────────────────────────────────
        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;
    }
}

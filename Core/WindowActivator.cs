using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MonitorAltTab.Core
{
    /// <summary>
    /// Handles activating/focusing a window reliably, including
    /// restoring minimized windows and stealing focus.
    /// </summary>
    public static class WindowActivator
    {
        /// <summary>
        /// Activates the specified window, bringing it to the foreground.
        /// Handles minimized windows and focus stealing prevention.
        /// </summary>
        public static void ActivateWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                Debug.WriteLine("[WindowActivator] Cannot activate null window.");
                return;
            }

            try
            {
                // If minimized, restore it first
                if (NativeMethods.IsIconic(hWnd))
                {
                    NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                    Debug.WriteLine("[WindowActivator] Restored minimized window.");
                }

                // Attach to the foreground window's thread to allow SetForegroundWindow
                IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
                uint foregroundThread = 0;
                uint targetThread = 0;
                uint currentThread = NativeMethods.GetCurrentThreadId();

                if (foregroundWindow != IntPtr.Zero)
                {
                    foregroundThread = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out _);
                }
                targetThread = NativeMethods.GetWindowThreadProcessId(hWnd, out _);

                bool attached = false;
                bool attachedCurrent = false;

                if (foregroundThread != 0 && foregroundThread != currentThread)
                {
                    attached = NativeMethods.AttachThreadInput(currentThread, foregroundThread, true);
                }
                if (targetThread != 0 && targetThread != currentThread && targetThread != foregroundThread)
                {
                    attachedCurrent = NativeMethods.AttachThreadInput(currentThread, targetThread, true);
                }

                try
                {
                    // Bring to top and set foreground
                    NativeMethods.BringWindowToTop(hWnd);
                    NativeMethods.SetForegroundWindow(hWnd);
                    NativeMethods.SetActiveWindow(hWnd);
                    NativeMethods.SetFocus(hWnd);

                    Debug.WriteLine($"[WindowActivator] Activated window 0x{hWnd:X}");
                }
                finally
                {
                    // Detach threads
                    if (attached)
                    {
                        NativeMethods.AttachThreadInput(currentThread, foregroundThread, false);
                    }
                    if (attachedCurrent)
                    {
                        NativeMethods.AttachThreadInput(currentThread, targetThread, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowActivator] Error activating window: {ex.Message}");
            }
        }
    }
}

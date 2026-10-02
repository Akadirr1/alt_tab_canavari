using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MonitorAltTab.Core
{
    /// <summary>
    /// Low-level keyboard hook that captures Alt+Tab / Alt+Shift+Tab
    /// and prevents the default Windows Alt+Tab from firing.
    /// </summary>
    public class KeyboardHook : IDisposable
    {
        private IntPtr _hookId = IntPtr.Zero;
        private readonly NativeMethods.LowLevelKeyboardProc _hookProc;
        private bool _disposed;

        // State tracking
        private bool _altDown;
        private bool _shiftDown;
        private bool _sessionActive;

        /// <summary>Fired when Alt+Tab is pressed (first or subsequent).</summary>
        public event Action<bool>? AltTabPressed; // bool isShiftHeld

        /// <summary>Fired when Alt is released.</summary>
        public event Action? AltReleased;

        /// <summary>Fired when Escape is pressed during a session.</summary>
        public event Action? EscapePressed;

        public bool IsSessionActive => _sessionActive;

        public KeyboardHook()
        {
            // Must keep a reference to the delegate to prevent GC
            _hookProc = HookCallback;
        }

        /// <summary>Install the low-level keyboard hook.</summary>
        public void Install()
        {
            if (_hookId != IntPtr.Zero) return;

            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;
            _hookId = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_KEYBOARD_LL,
                _hookProc,
                NativeMethods.GetModuleHandle(curModule.ModuleName),
                0);

            if (_hookId == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to install keyboard hook. Error: {error}");
            }

            Debug.WriteLine("[KeyboardHook] Hook installed successfully.");
        }

        /// <summary>Uninstall the keyboard hook.</summary>
        public void Uninstall()
        {
            if (_hookId != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
                _altDown = false;
                _shiftDown = false;
                _sessionActive = false;
                Debug.WriteLine("[KeyboardHook] Hook uninstalled.");
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var hookStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
                int msg = wParam.ToInt32();
                uint vk = hookStruct.vkCode;

                bool isKeyDown = msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN;
                bool isKeyUp = msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP;

                // Track Shift state
                if (vk == NativeMethods.VK_LSHIFT || vk == NativeMethods.VK_RSHIFT || vk == NativeMethods.VK_SHIFT)
                {
                    _shiftDown = isKeyDown;
                }

                // Track Alt state
                if (vk == NativeMethods.VK_LMENU || vk == NativeMethods.VK_RMENU || vk == NativeMethods.VK_MENU)
                {
                    if (isKeyDown)
                    {
                        _altDown = true;
                    }
                    else if (isKeyUp)
                    {
                        _altDown = false;

                        if (_sessionActive)
                        {
                            _sessionActive = false;
                            Debug.WriteLine("[KeyboardHook] Alt released → ending session.");
                            AltReleased?.Invoke();
                        }
                    }
                }

                // Alt+Tab / Alt+Shift+Tab
                if (vk == NativeMethods.VK_TAB && isKeyDown && _altDown)
                {
                    _sessionActive = true;
                    Debug.WriteLine($"[KeyboardHook] Alt+Tab detected. Shift={_shiftDown}");

                    // Fire event on a dispatcher-safe way
                    AltTabPressed?.Invoke(_shiftDown);

                    // Swallow the key to prevent Windows Alt+Tab
                    return (IntPtr)1;
                }

                // Escape during session → cancel
                if (vk == NativeMethods.VK_ESCAPE && isKeyDown && _sessionActive)
                {
                    _sessionActive = false;
                    _altDown = false;
                    Debug.WriteLine("[KeyboardHook] Escape → cancelling session.");
                    EscapePressed?.Invoke();
                    return (IntPtr)1;
                }

                // Swallow Alt keystrokes while session is active to prevent menu activation
                if (_sessionActive && (vk == NativeMethods.VK_LMENU || vk == NativeMethods.VK_RMENU || vk == NativeMethods.VK_MENU))
                {
                    if (isKeyUp)
                    {
                        _altDown = false;
                        _sessionActive = false;
                        Debug.WriteLine("[KeyboardHook] Alt released (swallowed) → ending session.");
                        AltReleased?.Invoke();
                    }
                    return (IntPtr)1;
                }
            }

            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Uninstall();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~KeyboardHook()
        {
            Dispose();
        }
    }
}

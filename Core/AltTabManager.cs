using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using MonitorAltTab.Models;
using MonitorAltTab.UI;
using Application = System.Windows.Application;

namespace MonitorAltTab.Core
{
    /// <summary>
    /// Core Alt+Tab session manager. Coordinates keyboard hook events,
    /// window enumeration, monitor detection, and overlay display.
    /// Maintains session state (selected index, window list, target monitor).
    /// </summary>
    public class AltTabManager : IDisposable
    {
        private readonly KeyboardHook _keyboardHook;
        private readonly WindowEnumerator _windowEnumerator;
        private AltTabOverlay? _overlay;
        private bool _disposed;

        // Session state
        private bool _isSessionActive;
        private IntPtr _sessionMonitor;
        private List<WindowInfo> _sessionWindows = new();
        private int _selectedIndex;

        public AltTabManager()
        {
            _keyboardHook = new KeyboardHook();
            _windowEnumerator = new WindowEnumerator();

            _keyboardHook.AltTabPressed += OnAltTabPressed;
            _keyboardHook.AltReleased += OnAltReleased;
            _keyboardHook.EscapePressed += OnEscapePressed;
        }

        /// <summary>Start intercepting Alt+Tab.</summary>
        public void Start()
        {
            _keyboardHook.Install();
            Debug.WriteLine("[AltTabManager] Started.");
        }

        /// <summary>Stop intercepting Alt+Tab.</summary>
        public void Stop()
        {
            EndSession(activateWindow: false);
            _keyboardHook.Uninstall();
            Debug.WriteLine("[AltTabManager] Stopped.");
        }

        /// <summary>
        /// Registers the overlay window handle so it's excluded from window enumeration.
        /// </summary>
        public void RegisterOverlay(AltTabOverlay overlay)
        {
            _overlay = overlay;
            var hwnd = new System.Windows.Interop.WindowInteropHelper(overlay).EnsureHandle();
            _windowEnumerator.SetOverlayHandle(hwnd);

            // Subscribe to mouse click events on overlay items
            _overlay.WindowItemClicked += OnWindowItemClicked;

            Debug.WriteLine($"[AltTabManager] Overlay registered: 0x{hwnd:X}");
        }

        /// <summary>
        /// Handles mouse click on an overlay window item.
        /// </summary>
        private void OnWindowItemClicked(int index)
        {
            if (!_isSessionActive || index < 0 || index >= _sessionWindows.Count)
                return;

            _selectedIndex = index;
            Debug.WriteLine($"[AltTabManager] Window clicked → index {index}: {_sessionWindows[index].Title}");
            EndSession(activateWindow: true);
        }

        private void OnAltTabPressed(bool isShiftHeld)
        {
            // Dispatch to UI thread
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (!_isSessionActive)
                {
                    StartSession();
                }

                if (_sessionWindows.Count == 0) return;

                // Move selection
                if (isShiftHeld)
                {
                    _selectedIndex = (_selectedIndex - 1 + _sessionWindows.Count) % _sessionWindows.Count;
                }
                else
                {
                    _selectedIndex = (_selectedIndex + 1) % _sessionWindows.Count;
                }

                Debug.WriteLine($"[AltTabManager] Selection → index {_selectedIndex}: {_sessionWindows[_selectedIndex].Title}");
                _overlay?.UpdateSelection(_selectedIndex);
            });
        }

        private void OnAltReleased()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                EndSession(activateWindow: true);
            });
        }

        private void OnEscapePressed()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                EndSession(activateWindow: false);
            });
        }

        private void StartSession()
        {
            // 1. Get cursor monitor
            _sessionMonitor = MonitorManager.GetMonitorFromCursor();
            var monitorInfo = MonitorManager.GetMonitorInfo(_sessionMonitor);

            Debug.WriteLine($"[AltTabManager] Starting session on monitor: {monitorInfo}");

            // 2. Enumerate windows on this monitor
            _sessionWindows = _windowEnumerator.GetWindowsOnMonitor(_sessionMonitor);

            if (_sessionWindows.Count == 0)
            {
                Debug.WriteLine("[AltTabManager] No windows found on this monitor. Aborting session.");
                return;
            }

            // 3. Set initial selection:
            //    Start at index 0 (which will be moved to 1 on the first Tab press in OnAltTabPressed)
            //    Actually, since OnAltTabPressed will increment, we start at -1 equivalent
            //    But the flow is: StartSession sets index=0, then OnAltTabPressed increments to 1
            //    For the natural Alt+Tab behavior, user expects to see the second window selected
            //    (the most recently used one after the current foreground)
            //    So we start at 0 — the first Tab press will move to index 1, then 2, etc.
            //    But wait — the caller already calls StartSession then increments. Let's start at 0.
            _selectedIndex = 0;
            _isSessionActive = true;

            // 4. Show overlay
            if (_overlay != null && monitorInfo != null)
            {
                _overlay.ShowWithWindows(_sessionWindows, _selectedIndex, monitorInfo);
            }

            Debug.WriteLine($"[AltTabManager] Session started. {_sessionWindows.Count} windows.");
        }

        private void EndSession(bool activateWindow)
        {
            if (!_isSessionActive) return;

            _isSessionActive = false;

            // Hide overlay
            _overlay?.HideOverlay();

            if (activateWindow && _sessionWindows.Count > 0 && _selectedIndex >= 0 && _selectedIndex < _sessionWindows.Count)
            {
                var selectedWindow = _sessionWindows[_selectedIndex];
                Debug.WriteLine($"[AltTabManager] Activating: {selectedWindow.Title}");
                WindowActivator.ActivateWindow(selectedWindow.Handle);
            }

            _sessionWindows.Clear();
            _selectedIndex = 0;
            _sessionMonitor = IntPtr.Zero;

            Debug.WriteLine("[AltTabManager] Session ended.");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _keyboardHook.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~AltTabManager()
        {
            Dispose();
        }
    }
}

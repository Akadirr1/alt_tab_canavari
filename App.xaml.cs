using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows;
using MonitorAltTab.Core;
using MonitorAltTab.UI;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace MonitorAltTab
{
    /// <summary>
    /// Application entry point. Sets up tray icon, overlay, and Alt+Tab manager.
    /// </summary>
    public partial class App : Application
    {
        private AltTabManager? _altTabManager;
        private AltTabOverlay? _overlay;
        private System.Windows.Forms.NotifyIcon? _trayIcon;
        private bool _isEnabled = true;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Prevent multiple instances
            string mutexName = "MonitorAltTab_SingleInstance";
            bool createdNew;
            var mutex = new System.Threading.Mutex(true, mutexName, out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("MonitorAltTab is already running.", "MonitorAltTab", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            Debug.WriteLine("[App] Starting MonitorAltTab...");

            // Create the overlay (hidden initially)
            _overlay = new AltTabOverlay();
            _overlay.Show(); // Must be shown once to get HWND, then hidden
            _overlay.Visibility = Visibility.Collapsed;

            // Create and start the manager
            _altTabManager = new AltTabManager();
            _altTabManager.RegisterOverlay(_overlay);
            _altTabManager.Start();

            // Setup tray icon
            SetupTrayIcon();

            Debug.WriteLine("[App] MonitorAltTab started successfully.");
        }

        private void SetupTrayIcon()
        {
            _trayIcon = new System.Windows.Forms.NotifyIcon();

            // Create a simple icon programmatically
            _trayIcon.Icon = CreateTrayIcon();
            _trayIcon.Text = "MonitorAltTab - Per-Monitor Alt+Tab";
            _trayIcon.Visible = true;

            // Context menu
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            // Status item
            var statusItem = new System.Windows.Forms.ToolStripMenuItem("✓ Enabled")
            {
                Name = "statusItem",
                Enabled = true,
            };
            statusItem.Click += (s, e) => ToggleEnabled(statusItem);
            contextMenu.Items.Add(statusItem);

            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            // About
            var aboutItem = new System.Windows.Forms.ToolStripMenuItem("About MonitorAltTab");
            aboutItem.Click += (s, e) =>
            {
                MessageBox.Show(
                    "MonitorAltTab v1.0\n\n" +
                    "Per-monitor Alt+Tab switcher.\n" +
                    "Alt+Tab only shows windows on the monitor\n" +
                    "where your mouse cursor is.\n\n" +
                    "Shortcuts:\n" +
                    "• Alt+Tab: Switch forward\n" +
                    "• Alt+Shift+Tab: Switch backward\n" +
                    "• Escape: Cancel\n\n" +
                    "Built with .NET 8 + WPF",
                    "About MonitorAltTab",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            };
            contextMenu.Items.Add(aboutItem);

            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            // Exit
            var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = contextMenu;
            _trayIcon.DoubleClick += (s, e) => ToggleEnabled(statusItem);
        }

        private void ToggleEnabled(System.Windows.Forms.ToolStripMenuItem statusItem)
        {
            _isEnabled = !_isEnabled;

            if (_isEnabled)
            {
                _altTabManager?.Start();
                statusItem.Text = "✓ Enabled";
                _trayIcon!.Text = "MonitorAltTab - Per-Monitor Alt+Tab";
                Debug.WriteLine("[App] Alt+Tab hook enabled.");
            }
            else
            {
                _altTabManager?.Stop();
                statusItem.Text = "✗ Disabled";
                _trayIcon!.Text = "MonitorAltTab - DISABLED";
                Debug.WriteLine("[App] Alt+Tab hook disabled.");
            }
        }

        private void ExitApplication()
        {
            Debug.WriteLine("[App] Exiting...");

            _altTabManager?.Dispose();
            _altTabManager = null;

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            _overlay?.Close();
            _overlay = null;

            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _altTabManager?.Dispose();

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }

            base.OnExit(e);
        }

        /// <summary>
        /// Creates a simple tray icon programmatically.
        /// </summary>
        private static Icon CreateTrayIcon()
        {
            // Create a 32x32 icon with a simple design
            using var bmp = new Bitmap(32, 32);
            using var g = System.Drawing.Graphics.FromImage(bmp);

            // Background
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(System.Drawing.Color.Transparent);

            // Draw a rounded rect with gradient-like look
            using var bgBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(220, 50, 100, 220));
            g.FillRectangle(bgBrush, 2, 2, 28, 28);

            // Draw "M" letter
            using var font = new System.Drawing.Font("Segoe UI", 16, System.Drawing.FontStyle.Bold);
            using var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
            var sf = new System.Drawing.StringFormat
            {
                Alignment = System.Drawing.StringAlignment.Center,
                LineAlignment = System.Drawing.StringAlignment.Center
            };
            g.DrawString("M", font, textBrush, new System.Drawing.RectangleF(0, 0, 32, 32), sf);

            // Convert to icon
            IntPtr hIcon = bmp.GetHicon();
            return System.Drawing.Icon.FromHandle(hIcon);
        }
    }
}

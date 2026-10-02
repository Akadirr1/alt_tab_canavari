using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using MonitorAltTab.Core;
using MonitorAltTab.UI;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace MonitorAltTab
{
    /// <summary>
    /// Application entry point. Sets up tray icon, overlay, control panel, and Alt+Tab manager.
    /// </summary>
    public partial class App : Application
    {
        private AltTabManager? _altTabManager;
        private AltTabOverlay? _overlay;
        private ControlPanelWindow? _controlPanel;
        private System.Windows.Forms.NotifyIcon? _trayIcon;
        private System.Windows.Forms.ToolStripMenuItem? _statusMenuItem;
        private AppSettings _settings = new();
        private Mutex? _instanceMutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Prevent multiple instances
            const string mutexName = "AltTabCanavari_SingleInstance_Mutex";
            _instanceMutex = new Mutex(true, mutexName, out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show(
                    "Alt Tab Canavarı zaten çalışıyor!\n\nEkranın sağ altındaki sistem tepsisi (saat yanı) simgesine çift tıklayarak Kontrol Panelini açabilirsiniz.",
                    "Alt Tab Canavarı",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown();
                return;
            }

            Debug.WriteLine("[App] Starting Alt Tab Canavarı...");

            // Load settings
            _settings = AppSettings.Load();

            // Create the overlay (hidden initially)
            _overlay = new AltTabOverlay();
            _overlay.Show();
            _overlay.Visibility = Visibility.Collapsed;

            // Create and start the manager
            _altTabManager = new AltTabManager();
            _altTabManager.RegisterOverlay(_overlay);

            if (_settings.IsEnabled)
            {
                _altTabManager.Start();
            }

            // Create Control Panel window
            _controlPanel = new ControlPanelWindow(_altTabManager, _settings);
            _controlPanel.RequestExit += ExitApplication;
            _controlPanel.HookStateChanged += OnHookStateChanged;

            // Setup tray icon
            SetupTrayIcon();

            // Determine if started minimized
            bool startMinimized = e.Args.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase))
                                  || _settings.StartMinimized;

            if (startMinimized)
            {
                _trayIcon?.ShowBalloonTip(3000, "Alt Tab Canavarı", "Arka planda ve sistem tepsisinde çalışıyor.", System.Windows.Forms.ToolTipIcon.Info);
            }
            else
            {
                _controlPanel.Show();
            }

            Debug.WriteLine("[App] Alt Tab Canavarı started successfully.");
        }

        private void SetupTrayIcon()
        {
            _trayIcon = new System.Windows.Forms.NotifyIcon();

            // Load icon
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app.ico");
                if (File.Exists(iconPath))
                {
                    _trayIcon.Icon = new Icon(iconPath);
                }
                else
                {
                    // Extract from process executable
                    string? currentExe = Environment.ProcessPath;
                    _trayIcon.Icon = !string.IsNullOrEmpty(currentExe)
                        ? Icon.ExtractAssociatedIcon(currentExe) ?? CreateFallbackIcon()
                        : CreateFallbackIcon();
                }
            }
            catch
            {
                _trayIcon.Icon = CreateFallbackIcon();
            }

            _trayIcon.Text = _settings.IsEnabled ? "Alt Tab Canavarı - Çalışıyor" : "Alt Tab Canavarı - Duraklatıldı";
            _trayIcon.Visible = true;

            // Context menu
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            // Open Control Panel
            var openItem = new System.Windows.Forms.ToolStripMenuItem("⚡ Kontrol Panelini Aç")
            {
                Font = new Font(contextMenu.Font, System.Drawing.FontStyle.Bold)
            };
            openItem.Click += (s, e) => ShowControlPanel();
            contextMenu.Items.Add(openItem);

            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            // Status toggle item
            _statusMenuItem = new System.Windows.Forms.ToolStripMenuItem(
                _settings.IsEnabled ? "✓ Devrede (Aktif)" : "✗ Duraklatıldı");
            _statusMenuItem.Click += (s, e) => ToggleHookState(!_settings.IsEnabled);
            contextMenu.Items.Add(_statusMenuItem);

            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            // Exit
            var exitItem = new System.Windows.Forms.ToolStripMenuItem("❌ Çıkış Yap");
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = contextMenu;

            // Double click opens control panel
            _trayIcon.DoubleClick += (s, e) => ShowControlPanel();
        }

        public void ShowControlPanel()
        {
            if (_controlPanel == null) return;

            _controlPanel.Show();
            if (_controlPanel.WindowState == WindowState.Minimized)
            {
                _controlPanel.WindowState = WindowState.Normal;
            }
            _controlPanel.Activate();
            _controlPanel.RefreshMonitorStatus();
        }

        private void OnHookStateChanged(bool isEnabled)
        {
            ToggleHookState(isEnabled, updateControlPanel: false);
        }

        private void ToggleHookState(bool isEnabled, bool updateControlPanel = true)
        {
            _settings.IsEnabled = isEnabled;
            _settings.Save();

            if (isEnabled)
            {
                _altTabManager?.Start();
                if (_statusMenuItem != null) _statusMenuItem.Text = "✓ Devrede (Aktif)";
                if (_trayIcon != null) _trayIcon.Text = "Alt Tab Canavarı - Çalışıyor";
                _trayIcon?.ShowBalloonTip(2000, "Alt Tab Canavarı", "Aktif edildi, Alt+Tab devrede.", System.Windows.Forms.ToolTipIcon.Info);
            }
            else
            {
                _altTabManager?.Stop();
                if (_statusMenuItem != null) _statusMenuItem.Text = "✗ Duraklatıldı";
                if (_trayIcon != null) _trayIcon.Text = "Alt Tab Canavarı - Duraklatıldı";
                _trayIcon?.ShowBalloonTip(2000, "Alt Tab Canavarı", "Duraklatıldı. Standart Windows Alt+Tab kullanılacak.", System.Windows.Forms.ToolTipIcon.Warning);
            }

            if (updateControlPanel && _controlPanel != null)
            {
                _controlPanel.UpdateHookState(isEnabled);
            }
        }

        private void ExitApplication()
        {
            Debug.WriteLine("[App] Exiting Alt Tab Canavarı...");

            _altTabManager?.Dispose();
            _altTabManager = null;

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            _controlPanel?.RealClose();
            _controlPanel = null;

            _overlay?.Close();
            _overlay = null;

            _instanceMutex?.ReleaseMutex();
            _instanceMutex?.Dispose();
            _instanceMutex = null;

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

            _instanceMutex?.Dispose();
            base.OnExit(e);
        }

        private static Icon CreateFallbackIcon()
        {
            using var bmp = new Bitmap(32, 32);
            using var g = Graphics.FromImage(bmp);
            g.Clear(System.Drawing.Color.Transparent);
            using var brush = new SolidBrush(System.Drawing.Color.FromArgb(99, 102, 241));
            g.FillEllipse(brush, 2, 2, 28, 28);
            IntPtr hIcon = bmp.GetHicon();
            return Icon.FromHandle(hIcon);
        }
    }
}

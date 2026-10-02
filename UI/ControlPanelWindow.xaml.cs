using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using MonitorAltTab.Core;
using MonitorAltTab.Models;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;

namespace MonitorAltTab.UI
{
    public class MonitorDisplayItem
    {
        public string Name { get; set; } = "";
        public string ResolutionText { get; set; } = "";
        public string ActiveWindowsText { get; set; } = "";
        public string IconSymbol => "🖥️";
        public Visibility PrimaryBadgeVisibility { get; set; } = Visibility.Collapsed;
        public Visibility CursorHereBadgeVisibility { get; set; } = Visibility.Collapsed;
        public Brush CardBackground { get; set; } = new SolidColorBrush(Color.FromRgb(0x18, 0x22, 0x38));
        public Brush CardBorderBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0x27, 0x35, 0x4D));
    }

    public partial class ControlPanelWindow : Window
    {
        private readonly AltTabManager _altTabManager;
        private readonly WindowEnumerator _windowEnumerator;
        private readonly AppSettings _settings;
        private readonly DispatcherTimer _refreshTimer;
        private bool _isRealExit = false;

        public event Action? RequestExit;
        public event Action<bool>? HookStateChanged;

        public ControlPanelWindow(AltTabManager altTabManager, AppSettings settings)
        {
            InitializeComponent();

            _altTabManager = altTabManager;
            _settings = settings;
            _windowEnumerator = new WindowEnumerator();

            // Bind settings to UI
            ChkEnableHook.IsChecked = _settings.IsEnabled;
            ChkAutoStart.IsChecked = _settings.AutoStartWithWindows;
            ChkMinimizeToTray.IsChecked = _settings.MinimizeToTrayOnClose;
            ChkStartMinimized.IsChecked = _settings.StartMinimized;

            UpdateStatusPill(_settings.IsEnabled);

            // Setup timer for live monitor & cursor refresh
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _refreshTimer.Tick += (s, e) => RefreshMonitorStatus();
            _refreshTimer.Start();

            RefreshMonitorStatus();
        }

        public void UpdateHookState(bool isEnabled)
        {
            _settings.IsEnabled = isEnabled;
            ChkEnableHook.IsChecked = isEnabled;
            UpdateStatusPill(isEnabled);
            _settings.Save();
        }

        private void UpdateStatusPill(bool isEnabled)
        {
            if (isEnabled)
            {
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0x06, 0x4E, 0x3B));
                StatusBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(0x05, 0x96, 0x69));
                StatusDot.Fill = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
                StatusText.Text = "ÇALIŞIYOR";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x6E, 0xE7, 0xB7));
            }
            else
            {
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0x45, 0x0A, 0x0A));
                StatusBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
                StatusDot.Fill = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                StatusText.Text = "DURAKLATILDI";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xFC, 0xA5, 0xA5));
            }
        }

        public void RefreshMonitorStatus()
        {
            if (!IsVisible) return;

            try
            {
                var monitors = MonitorManager.GetAllMonitors();
                IntPtr activeCursorMonitor = MonitorManager.GetMonitorFromCursor();

                var displayItems = new List<MonitorDisplayItem>();

                for (int i = 0; i < monitors.Count; i++)
                {
                    var mon = monitors[i];
                    bool isCursorHere = (mon.Handle == activeCursorMonitor);

                    int width = mon.MonitorRect.Right - mon.MonitorRect.Left;
                    int height = mon.MonitorRect.Bottom - mon.MonitorRect.Top;

                    // Count open windows on this monitor
                    var windows = _windowEnumerator.GetWindowsOnMonitor(mon.Handle);

                    var item = new MonitorDisplayItem
                    {
                        Name = $"Ekran {i + 1} ({mon.DeviceName})",
                        ResolutionText = $"{width} × {height}",
                        ActiveWindowsText = $"{windows.Count} açık uygulama penceresi",
                        PrimaryBadgeVisibility = mon.IsPrimary ? Visibility.Visible : Visibility.Collapsed,
                        CursorHereBadgeVisibility = isCursorHere ? Visibility.Visible : Visibility.Collapsed,
                        CardBackground = isCursorHere
                            ? new SolidColorBrush(Color.FromArgb(0x40, 0x02, 0x84, 0xC7))
                            : new SolidColorBrush(Color.FromRgb(0x13, 0x1B, 0x2E)),
                        CardBorderBrush = isCursorHere
                            ? new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8))
                            : new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B))
                    };

                    displayItems.Add(item);
                }

                MonitorsItemsControl.ItemsSource = displayItems;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ControlPanel] RefreshMonitorStatus failed: {ex.Message}");
            }
        }

        private void OnSettingChanged(object sender, RoutedEventArgs e)
        {
            if (_settings == null) return;

            bool isEnabled = ChkEnableHook.IsChecked == true;
            _settings.IsEnabled = isEnabled;
            _settings.MinimizeToTrayOnClose = ChkMinimizeToTray.IsChecked == true;
            _settings.StartMinimized = ChkStartMinimized.IsChecked == true;
            _settings.Save();

            UpdateStatusPill(isEnabled);
            HookStateChanged?.Invoke(isEnabled);
        }

        private void OnAutoStartChanged(object sender, RoutedEventArgs e)
        {
            if (_settings == null) return;
            bool autoStart = ChkAutoStart.IsChecked == true;
            _settings.SetAutoStart(autoStart);
        }

        private void BtnTestOverlay_Click(object sender, RoutedEventArgs e)
        {
            // Simulate Alt+Tab trigger
            IntPtr cursorMonitor = MonitorManager.GetMonitorFromCursor();
            var windows = _windowEnumerator.GetWindowsOnMonitor(cursorMonitor);
            var monInfo = MonitorManager.GetMonitorInfo(cursorMonitor);

            if (windows.Count == 0)
            {
                MessageBox.Show("Bu monitörde açık pencere bulunamadı. Lütfen bir pencere açıp tekrar deneyin.", "Alt Tab Canavarı", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show($"Başarılı! Bu ekranda {windows.Count} adet pencere bulundu.\nKlavyeden Alt+Tab tuşlarına basarak canavarı hemen kullanabilirsiniz!", "Alt Tab Canavarı", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnInstallCert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string certPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AltTabCanavari.cer");
                if (!File.Exists(certPath))
                {
                    // Look in working directory
                    certPath = Path.Combine(Directory.GetCurrentDirectory(), "AltTabCanavari.cer");
                }

                if (!File.Exists(certPath))
                {
                    MessageBox.Show(
                        "Sertifika dosyası (AltTabCanavari.cer) bulunamadı.\n" +
                        "Uygulama klasöründe sertifika dosyasının olduğundan emin olun.",
                        "Sertifika Bulunamadı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Run certutil with elevated prompt
                var psi = new ProcessStartInfo
                {
                    FileName = "certutil.exe",
                    Arguments = $"-addstore -f \"Root\" \"{certPath}\"",
                    Verb = "runas", // UAC elevation
                    UseShellExecute = true
                };

                Process.Start(psi)?.WaitForExit();

                MessageBox.Show(
                    "Sertifika başarıyla Windows Güvenilen Kök Sertifika Yetkilileri'ne eklendi!\n" +
                    "Artık Windows SmartScreen uyarısı vermeyecektir.",
                    "Sertifika Başarıyla Yüklendi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Sertifika yüklenirken bir hata oluştu veya yetki reddedildi:\n{ex.Message}",
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnMinimizeToTray_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            _isRealExit = true;
            RequestExit?.Invoke();
        }

        public void RealClose()
        {
            _isRealExit = true;
            _refreshTimer.Stop();
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_isRealExit && (_settings?.MinimizeToTrayOnClose ?? true))
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                _refreshTimer.Stop();
                base.OnClosing(e);
            }
        }
    }
}

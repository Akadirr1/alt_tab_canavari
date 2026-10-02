using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MonitorAltTab.Core;
using MonitorAltTab.Models;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;

namespace MonitorAltTab.UI
{
    /// <summary>
    /// Custom Alt+Tab overlay window. Displays window thumbnails/icons
    /// with titles in a horizontal/grid layout centered on the target monitor.
    /// </summary>
    public partial class AltTabOverlay : Window
    {
        private const double ITEM_WIDTH = 160;
        private const double ITEM_HEIGHT = 130;
        private const double ITEM_MARGIN = 6;
        private const double MAX_ITEMS_PER_ROW = 8;
        private const double OVERLAY_PADDING = 20;

        private List<Border> _itemBorders = new();
        private int _selectedIndex = -1;

        /// <summary>Fired when a window item is clicked. Parameter is the index.</summary>
        public event Action<int>? WindowItemClicked;

        // Extended window styles to prevent focus stealing
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE_VAL = 0x08000000;
        private const int WS_EX_TOOLWINDOW_VAL = 0x00000080;

        public AltTabOverlay()
        {
            InitializeComponent();
            this.Visibility = Visibility.Collapsed;
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Set extended styles so the overlay never steals focus
            var hwnd = new WindowInteropHelper(this).Handle;
            var exStyle = NativeMethods.GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            exStyle |= WS_EX_NOACTIVATE_VAL;
            exStyle |= WS_EX_TOOLWINDOW_VAL;
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(exStyle));
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        /// <summary>
        /// Shows the overlay with the given windows on the specified monitor.
        /// </summary>
        public void ShowWithWindows(List<WindowInfo> windows, int selectedIndex, MonitorInfoModel monitor)
        {
            if (windows.Count == 0)
            {
                HideOverlay();
                return;
            }

            _selectedIndex = selectedIndex;
            BuildWindowItems(windows);
            PositionOnMonitor(monitor, windows.Count);
            HighlightSelected(selectedIndex);

            // Update monitor label
            MonitorLabel.Text = monitor.IsPrimary
                ? $"Primary Monitor • {windows.Count} windows"
                : $"{monitor.DeviceName.TrimEnd('\0')} • {windows.Count} windows";

            this.Visibility = Visibility.Visible;
            this.Opacity = 0;

            // Fade in
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120));
            fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            this.BeginAnimation(OpacityProperty, fadeIn);

            Debug.WriteLine($"[Overlay] Shown with {windows.Count} items, selected={selectedIndex}");
        }

        /// <summary>
        /// Updates the visual selection highlight.
        /// </summary>
        public void UpdateSelection(int newIndex)
        {
            if (newIndex < 0 || newIndex >= _itemBorders.Count)
                return;

            HighlightSelected(newIndex);
            _selectedIndex = newIndex;

            // Ensure selected item is visible in scroll viewer
            if (_itemBorders.Count > 0 && newIndex < _itemBorders.Count)
            {
                _itemBorders[newIndex].BringIntoView();
            }
        }

        /// <summary>
        /// Hides the overlay with a quick fade out.
        /// </summary>
        public void HideOverlay()
        {
            if (this.Visibility != Visibility.Visible) return;

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(80));
            fadeOut.Completed += (s, e) =>
            {
                this.Visibility = Visibility.Collapsed;
            };
            this.BeginAnimation(OpacityProperty, fadeOut);
        }

        /// <summary>
        /// Builds the visual items for each window.
        /// </summary>
        private void BuildWindowItems(List<WindowInfo> windows)
        {
            WindowItems.Items.Clear();
            _itemBorders.Clear();

            for (int i = 0; i < windows.Count; i++)
            {
                var item = CreateWindowItem(windows[i], i);
                WindowItems.Items.Add(item);
                _itemBorders.Add(item);
            }
        }

        /// <summary>
        /// Creates a single window item visual element.
        /// </summary>
        private Border CreateWindowItem(WindowInfo window, int index)
        {
            // Main container
            var border = new Border
            {
                Width = ITEM_WIDTH,
                Height = ITEM_HEIGHT,
                Margin = new Thickness(ITEM_MARGIN),
                CornerRadius = new CornerRadius(12),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                SnapsToDevicePixels = true,
                Tag = index, // Store index for click/hover handlers
            };

            // Mouse click → activate this window
            border.MouseLeftButtonUp += (s, e) =>
            {
                if (s is Border b && b.Tag is int clickedIndex)
                {
                    Debug.WriteLine($"[Overlay] Item clicked: index {clickedIndex}");
                    _selectedIndex = clickedIndex;
                    HighlightSelected(clickedIndex);
                    WindowItemClicked?.Invoke(clickedIndex);
                    e.Handled = true;
                }
            };

            // Mouse hover → highlight this item
            border.MouseEnter += (s, e) =>
            {
                if (s is Border b && b.Tag is int hoverIndex)
                {
                    _selectedIndex = hoverIndex;
                    HighlightSelected(hoverIndex);
                }
            };

            // Inner stack
            var stack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(8),
                IsHitTestVisible = false, // Let clicks pass through to the border
            };

            // Icon
            if (window.Icon != null)
            {
                var iconImage = new Image
                {
                    Source = window.Icon,
                    Width = 48,
                    Height = 48,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 8),
                };
                RenderOptions.SetBitmapScalingMode(iconImage, BitmapScalingMode.HighQuality);
                stack.Children.Add(iconImage);
            }
            else
            {
                // Placeholder icon — first letter of title
                var placeholderText = new TextBlock
                {
                    Text = window.Title.Length > 0 ? window.Title[0].ToString().ToUpper() : "?",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(130, 180, 255)),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                var placeholder = new Border
                {
                    Width = 48,
                    Height = 48,
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(Color.FromArgb(60, 100, 140, 255)),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 8),
                    Child = placeholderText,
                };
                stack.Children.Add(placeholder);
            }

            // Title
            var title = new TextBlock
            {
                Text = TruncateTitle(window.Title, 20),
                FontSize = 11.5,
                FontWeight = FontWeights.Medium,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(220, 225, 235)),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = ITEM_WIDTH - 16,
                ToolTip = window.Title,
            };
            stack.Children.Add(title);

            // Process name subtitle
            if (!string.IsNullOrEmpty(window.ProcessName))
            {
                var subtitle = new TextBlock
                {
                    Text = window.ProcessName,
                    FontSize = 9.5,
                    Foreground = new SolidColorBrush(Color.FromArgb(140, 160, 170, 200)),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0),
                };
                stack.Children.Add(subtitle);
            }

            border.Child = stack;

            // Add minimized indicator
            if (window.IsMinimized)
            {
                border.Opacity = 0.6;
            }

            return border;
        }

        /// <summary>
        /// Highlights the selected item and dims others.
        /// </summary>
        private void HighlightSelected(int index)
        {
            for (int i = 0; i < _itemBorders.Count; i++)
            {
                var border = _itemBorders[i];

                if (i == index)
                {
                    // Selected state — vibrant accent
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 140, 255));
                    border.Background = new SolidColorBrush(Color.FromArgb(70, 60, 120, 255));

                    // Subtle glow effect
                    border.Effect = new DropShadowEffect
                    {
                        Color = Color.FromRgb(60, 120, 255),
                        BlurRadius = 20,
                        ShadowDepth = 0,
                        Opacity = 0.5,
                    };

                    // Scale animation
                    var scaleTransform = new ScaleTransform(1.0, 1.0);
                    border.RenderTransform = scaleTransform;
                    border.RenderTransformOrigin = new Point(0.5, 0.5);

                    var scaleUp = new DoubleAnimation(1.0, 1.05, TimeSpan.FromMilliseconds(100));
                    scaleUp.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);
                }
                else
                {
                    // Unselected state
                    border.BorderBrush = new SolidColorBrush(Colors.Transparent);
                    border.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                    border.Effect = null;

                    var scaleTransform = new ScaleTransform(1.0, 1.0);
                    border.RenderTransform = scaleTransform;
                    border.RenderTransformOrigin = new Point(0.5, 0.5);

                    var scaleDown = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(100));
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleDown);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleDown);
                }
            }
        }

        /// <summary>
        /// Positions the overlay centered on the target monitor.
        /// Accounts for DPI scaling and monitor work area.
        /// </summary>
        private void PositionOnMonitor(MonitorInfoModel monitor, int windowCount)
        {
            // Calculate the needed width based on items
            int itemsPerRow = (int)Math.Min(windowCount, MAX_ITEMS_PER_ROW);
            double contentWidth = itemsPerRow * (ITEM_WIDTH + ITEM_MARGIN * 2) + OVERLAY_PADDING * 2 + 10;
            int rows = (int)Math.Ceiling((double)windowCount / MAX_ITEMS_PER_ROW);
            double contentHeight = rows * (ITEM_HEIGHT + ITEM_MARGIN * 2) + OVERLAY_PADDING * 2 + 30;

            // Get DPI scaling — try multiple approaches for reliability
            double dpiScaleX = 1.0;
            double dpiScaleY = 1.0;

            try
            {
                // Best approach: use VisualTreeHelper.GetDpi (works in .NET 4.6.2+)
                var dpiInfo = VisualTreeHelper.GetDpi(this);
                dpiScaleX = dpiInfo.DpiScaleX;
                dpiScaleY = dpiInfo.DpiScaleY;
            }
            catch
            {
                // Fallback: try PresentationSource
                var presentationSource = PresentationSource.FromVisual(this);
                if (presentationSource?.CompositionTarget != null)
                {
                    dpiScaleX = presentationSource.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = presentationSource.CompositionTarget.TransformToDevice.M22;
                }
            }

            // Monitor rect is in physical pixels; WPF uses DIPs
            double monLeft = monitor.WorkArea.Left / dpiScaleX;
            double monTop = monitor.WorkArea.Top / dpiScaleY;
            double monWidth = monitor.WorkArea.Width / dpiScaleX;
            double monHeight = monitor.WorkArea.Height / dpiScaleY;

            // Cap content size to monitor
            contentWidth = Math.Min(contentWidth, monWidth * 0.9);
            contentHeight = Math.Min(contentHeight, monHeight * 0.7);

            // Center on monitor
            this.Left = monLeft + (monWidth - contentWidth) / 2;
            this.Top = monTop + (monHeight - contentHeight) / 2;
            this.Width = contentWidth;
            this.Height = contentHeight;

            Debug.WriteLine($"[Overlay] Positioned at ({this.Left}, {this.Top}) size ({this.Width}x{this.Height}) DPI=({dpiScaleX}, {dpiScaleY})");
        }

        /// <summary>
        /// Truncates a title to a maximum number of characters.
        /// </summary>
        private static string TruncateTitle(string title, int maxLength)
        {
            if (title.Length <= maxLength)
                return title;
            return title.Substring(0, maxLength - 1) + "…";
        }
    }
}

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace FishTankScreensaver
{
    public partial class ScreensaverWindow : Window
    {
        private Point? _initialMousePosition;
        private bool _isClosing;
        private readonly bool _isPreview;
        private DispatcherTimer? _inputPollTimer;

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        public ScreensaverWindow(bool isPreview = false)
        {
            InitializeComponent();
            _isPreview = isPreview;

            if (isPreview)
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Normal;
                Topmost = false;
                Cursor = Cursors.Arrow;
                Width = 800;
                Height = 500;
                Title = "Fish Tank Screensaver Preview";
                ResizeMode = ResizeMode.CanResize;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            Loaded += ScreensaverWindow_Loaded;
        }

        private async void ScreensaverWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Start polling for input at the OS level — this catches all input
                // even when WebView2 has focus
                if (!_isPreview)
                {
                    GetCursorPos(out var startPos);
                    _initialMousePosition = new Point(startPos.X, startPos.Y);

                    _inputPollTimer = new DispatcherTimer();
                    _inputPollTimer.Interval = TimeSpan.FromMilliseconds(100);
                    _inputPollTimer.Tick += InputPollTimer_Tick;
                    _inputPollTimer.Start();
                }

                var env = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: System.IO.Path.Combine(
                        System.IO.Path.GetTempPath(), "FishTankScreensaver_WebView2"));

                await WebView.EnsureCoreWebView2Async(env);

                WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                WebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;

                WebView.CoreWebView2.NavigationCompleted += async (s, args) =>
                {
                    // Kill scrollbars and disable all pointer interaction in the page
                    await WebView.CoreWebView2.ExecuteScriptAsync(@"
                        document.documentElement.style.overflow = 'hidden';
                        document.body.style.overflow = 'hidden';
                        document.body.style.margin = '0';
                        document.body.style.padding = '0';
                        document.body.style.pointerEvents = 'none';
                    ");
                    Dispatcher.Invoke(() => LoadingOverlay.Visibility = Visibility.Collapsed);
                };

                var settings = ScreensaverSettings.Load();
                WebView.CoreWebView2.Navigate(settings.BuildTankUrl());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize WebView2. Please ensure the WebView2 Runtime is installed.\n\n{ex.Message}",
                    "Fish Tank Screensaver",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Close();
            }
        }

        private void InputPollTimer_Tick(object? sender, EventArgs e)
        {
            // Check mouse movement
            if (GetCursorPos(out var pos) && _initialMousePosition.HasValue)
            {
                var dx = pos.X - _initialMousePosition.Value.X;
                var dy = pos.Y - _initialMousePosition.Value.Y;
                if (Math.Abs(dx) > 10 || Math.Abs(dy) > 10)
                {
                    CloseScreensaver();
                    return;
                }
            }

            // Check if any key or mouse button is pressed
            // Check mouse buttons (VK_LBUTTON=0x01, VK_RBUTTON=0x02, VK_MBUTTON=0x04)
            for (int vk = 0x01; vk <= 0x04; vk++)
            {
                if ((GetAsyncKeyState(vk) & 0x8000) != 0)
                {
                    CloseScreensaver();
                    return;
                }
            }

            // Check keyboard keys (0x08 through 0xFE covers all virtual key codes)
            for (int vk = 0x08; vk <= 0xFE; vk++)
            {
                if ((GetAsyncKeyState(vk) & 0x8000) != 0)
                {
                    CloseScreensaver();
                    return;
                }
            }
        }

        private void CloseScreensaver()
        {
            if (_isClosing) return;
            _isClosing = true;
            _inputPollTimer?.Stop();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _inputPollTimer?.Stop();
            base.OnClosed(e);
        }
    }
}

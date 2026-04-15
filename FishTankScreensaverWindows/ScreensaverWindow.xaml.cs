using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace FishTankScreensaver
{
    public partial class ScreensaverWindow : Window
    {
        private Point? _initialMousePosition;
        private bool _isClosing;
        private readonly bool _isPreview;

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
                // Remove the input-blocking overlay in preview mode
                InputOverlay.Visibility = Visibility.Collapsed;
            }

            // Keyboard events work at the window level regardless of WebView2
            PreviewKeyDown += Window_KeyDown;

            Loaded += ScreensaverWindow_Loaded;
        }

        private async void ScreensaverWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var env = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: System.IO.Path.Combine(
                        System.IO.Path.GetTempPath(), "FishTankScreensaver_WebView2"));

                await WebView.EnsureCoreWebView2Async(env);

                WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                WebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;

                // Once navigation completes, hide loading overlay and inject CSS to kill scrollbars
                WebView.CoreWebView2.NavigationCompleted += async (s, args) =>
                {
                    await WebView.CoreWebView2.ExecuteScriptAsync(@"
                        document.documentElement.style.overflow = 'hidden';
                        document.body.style.overflow = 'hidden';
                        document.body.style.margin = '0';
                        document.body.style.padding = '0';
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

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_isPreview)
                CloseScreensaver();
        }

        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isPreview)
                CloseScreensaver();
        }

        private void Overlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPreview) return;

            var currentPosition = e.GetPosition(this);

            if (_initialMousePosition == null)
            {
                _initialMousePosition = currentPosition;
                return;
            }

            var delta = currentPosition - _initialMousePosition.Value;
            if (Math.Abs(delta.X) > 10 || Math.Abs(delta.Y) > 10)
            {
                CloseScreensaver();
            }
        }

        private void Overlay_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!_isPreview)
                CloseScreensaver();
        }

        private void CloseScreensaver()
        {
            if (_isClosing) return;
            _isClosing = true;
            Close();
        }
    }
}

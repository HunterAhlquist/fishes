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

        public ScreensaverWindow()
        {
            InitializeComponent();
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

                // Disable all interaction - this is a screensaver
                WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                WebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;

                // Hide loading overlay once navigation completes
                WebView.CoreWebView2.NavigationCompleted += (s, args) =>
                {
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
            CloseScreensaver();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CloseScreensaver();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var currentPosition = e.GetPosition(this);

            if (_initialMousePosition == null)
            {
                _initialMousePosition = currentPosition;
                return;
            }

            // Only close if the mouse has moved a significant amount
            var delta = currentPosition - _initialMousePosition.Value;
            if (Math.Abs(delta.X) > 10 || Math.Abs(delta.Y) > 10)
            {
                CloseScreensaver();
            }
        }

        private void CloseScreensaver()
        {
            if (_isClosing) return;
            _isClosing = true;
            Close();
        }
    }
}

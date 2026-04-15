using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FishTankScreensaver
{
    public partial class ScreensaverWindow : Window
    {
        private Point? _initialMousePosition;
        private bool _isClosing;
        private readonly bool _isPreview;

        private readonly List<Fish> _fishes = new();
        private bool _isLoading;
        private bool _hasStartedLoading;
        private ScreensaverSettings _settings = null!;
        private Color _bgColor;

        private DispatcherTimer? _animationTimer;
        private DispatcherTimer? _inputPollTimer;
        private readonly DateTime _startTime = DateTime.UtcNow;

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

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

            _settings = ScreensaverSettings.Load();
            _bgColor = ParseHexColor(_settings.BackgroundColor);
            Background = new SolidColorBrush(_bgColor);

            Loaded += ScreensaverWindow_Loaded;
        }

        private static Color ParseHexColor(string hex)
        {
            try
            {
                if (hex.Length == 6)
                    return Color.FromRgb(
                        Convert.ToByte(hex.Substring(0, 2), 16),
                        Convert.ToByte(hex.Substring(2, 2), 16),
                        Convert.ToByte(hex.Substring(4, 2), 16));
            }
            catch { }
            return Color.FromRgb(224, 247, 250); // #e0f7fa
        }

        private void ScreensaverWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Start input polling for screensaver mode
            if (!_isPreview)
            {
                GetCursorPos(out var startPos);
                _initialMousePosition = new Point(startPos.X, startPos.Y);

                _inputPollTimer = new DispatcherTimer();
                _inputPollTimer.Interval = TimeSpan.FromMilliseconds(100);
                _inputPollTimer.Tick += InputPollTimer_Tick;
                _inputPollTimer.Start();
            }

            // Start animation loop at 60 FPS
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / 60.0);
            _animationTimer.Tick += AnimateOneFrame;
            _animationTimer.Start();
        }

        private double CurrentTime => (DateTime.UtcNow - _startTime).TotalSeconds;

        // --- Fish Loading ---

        private async void LoadFishFromAPI()
        {
            if (_isLoading) return;
            _isLoading = true;

            var fishDataList = await FishAPI.FetchFishAsync(_settings.SortType, _settings.FishCount);

            var validFish = fishDataList.FindAll(f => f.ImageURL != null && f.ImageURL.StartsWith("http"));
            if (validFish.Count == 0)
            {
                _isLoading = false;
                StatusText.Text = "No fish available.";
                return;
            }

            var fishSize = CalculateFishSize();
            double cw = RootGrid.ActualWidth;
            double ch = RootGrid.ActualHeight;

            var newFishes = new List<Fish>();

            foreach (var fishData in validFish)
            {
                var imageBytes = await FishAPI.LoadImageDataAsync(fishData.ImageURL!);
                if (imageBytes == null) continue;

                try
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = new MemoryStream(imageBytes);
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    var fish = new Fish(bitmapImage, cw, ch,
                        fishSize.Width, fishSize.Height,
                        fishData.ArtistName, fishData.Id, fishData.Score);
                    newFishes.Add(fish);
                }
                catch { }
            }

            _fishes.Clear();
            _fishes.AddRange(newFishes);
            _isLoading = false;

            if (_fishes.Count > 0)
                StatusText.Visibility = Visibility.Collapsed;
            else
                StatusText.Text = "No fish available.";
        }

        private Size CalculateFishSize()
        {
            double baseDimension = Math.Min(RootGrid.ActualWidth, RootGrid.ActualHeight);
            double fishWidth = Math.Floor(baseDimension * 0.1);
            double fishHeight = Math.Floor(fishWidth * 0.6);
            return new Size(
                Math.Max(30, Math.Min(150, fishWidth)),
                Math.Max(18, Math.Min(90, fishHeight)));
        }

        // --- Animation ---

        private void AnimateOneFrame(object? sender, EventArgs e)
        {
            double cw = RootGrid.ActualWidth;
            double ch = RootGrid.ActualHeight;

            if (!_hasStartedLoading && cw > 0 && ch > 0)
            {
                _hasStartedLoading = true;
                LoadFishFromAPI();
            }

            double time = CurrentTime / 0.5; // Match macOS: CACurrentMediaTime() / 0.5

            foreach (var fish in _fishes)
            {
                fish.UpdatePhysics(cw, ch);
                fish.UpdateEntrance(CurrentTime);
                fish.UpdateDeath(CurrentTime);
            }
            _fishes.RemoveAll(f => f.IsDeathComplete(CurrentTime));

            // Render frame
            if (cw <= 0 || ch <= 0) return;

            int pw = (int)cw;
            int ph = (int)ch;

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                // Fill background
                dc.DrawRectangle(new SolidColorBrush(_bgColor), null, new Rect(0, 0, pw, ph));

                // Draw fish
                foreach (var fish in _fishes)
                {
                    fish.Draw(dc, time);
                }
            }

            var rtb = new RenderTargetBitmap(pw, ph, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();

            FrameImage.Source = rtb;
        }

        // --- Input Handling ---

        private void InputPollTimer_Tick(object? sender, EventArgs e)
        {
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

            // Check mouse buttons
            for (int vk = 0x01; vk <= 0x04; vk++)
            {
                if ((GetAsyncKeyState(vk) & 0x8000) != 0)
                {
                    CloseScreensaver();
                    return;
                }
            }

            // Check keyboard
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
            _animationTimer?.Stop();
            _inputPollTimer?.Stop();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _animationTimer?.Stop();
            _inputPollTimer?.Stop();
            base.OnClosed(e);
        }
    }
}

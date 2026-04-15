using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FishTankScreensaver
{
    public class Fish
    {
        // Source fish pixel data (BGRA, row-major)
        private byte[] _pixelData = Array.Empty<byte>();
        private int _pixelWidth;
        private int _pixelHeight;

        // Scratch buffer for the wiggled fish (wider to accommodate tail movement)
        private byte[] _wiggleBuffer = Array.Empty<byte>();
        private int _wiggleWidth;
        private int _wiggleHeight;

        public double X;
        public double Y;
        public double Direction;
        public double Vx;
        public double Vy;
        public double Phase;
        public double Amplitude;
        public double Speed;
        public double Width;
        public double Height;
        public string Artist;
        public string? DocId;
        public double Peduncle;
        public int Score;

        public bool IsDying;
        public bool IsEntering;
        public double Opacity = 1.0;
        public double Scale = 1.0;
        private double _deathStartTime;
        private double _enterStartTime;
        private double _originalY;

        private static readonly Random Rng = new();

        public Fish(BitmapSource image, double canvasWidth, double canvasHeight,
                    double fishWidth, double fishHeight,
                    string artist = "Anonymous", string? docId = null, int score = 0)
        {
            Width = fishWidth;
            Height = fishHeight;
            Artist = artist;
            DocId = docId;
            Score = score;
            Peduncle = 0.4;
            Phase = Rng.NextDouble() * 2 * Math.PI;
            Amplitude = 20 + Rng.NextDouble() * 12; // 20..32
            Speed = 1.5 + Rng.NextDouble(); // 1.5..2.5
            Direction = Rng.Next(2) == 0 ? 1 : -1;
            X = Rng.NextDouble() * Math.Max(1, canvasWidth - fishWidth);
            Y = Rng.NextDouble() * Math.Max(1, canvasHeight - fishHeight);
            Vx = Speed * Direction * 0.1;
            Vy = (Rng.NextDouble() - 0.5) * 0.5; // -0.25..0.25

            _pixelWidth = (int)fishWidth;
            _pixelHeight = (int)fishHeight;

            int wiggleMargin = 14;
            _wiggleWidth = _pixelWidth + wiggleMargin * 2;
            _wiggleHeight = _pixelHeight;

            ExtractPixelData(image);
            _wiggleBuffer = new byte[_wiggleWidth * _wiggleHeight * 4];
        }

        private void ExtractPixelData(BitmapSource source)
        {
            int w = _pixelWidth, h = _pixelHeight;
            if (w <= 0 || h <= 0) return;

            // Scale the source image to fish size
            var scaled = new TransformedBitmap(source,
                new ScaleTransform(
                    (double)w / source.PixelWidth,
                    (double)h / source.PixelHeight));

            // Convert to BGRA32
            var converted = new FormatConvertedBitmap(scaled, PixelFormats.Bgra32, null, 0);

            _pixelData = new byte[w * h * 4];
            converted.CopyPixels(_pixelData, w * 4, 0);
        }

        /// Build the wiggled fish frame by copying columns with offset
        private WriteableBitmap? BuildWiggleFrame(double time)
        {
            int w = _pixelWidth, h = _pixelHeight;
            if (w <= 0 || h <= 0 || _pixelData.Length != w * h * 4) return null;

            int tailEnd = (int)(w * Peduncle);
            int margin = (_wiggleWidth - w) / 2;
            double timeFactor = time * 3 + Phase;

            // Clear wiggle buffer
            Array.Clear(_wiggleBuffer, 0, _wiggleBuffer.Length);

            // Copy each column with wiggle offset
            for (int i = 0; i < w; i++)
            {
                double t, wiggle;
                int srcCol, dstBaseX;

                if (Direction >= 0)
                {
                    bool isTail = i < tailEnd;
                    t = isTail ? (double)(tailEnd - i - 1) / Math.Max(tailEnd - 1, 1) : 0;
                    wiggle = isTail ? Math.Sin(timeFactor + t * 2) * t * 12 : 0;
                    srcCol = i;
                    dstBaseX = margin + i + (int)Math.Round(wiggle);
                }
                else
                {
                    bool isTail = i >= w - tailEnd;
                    t = isTail ? (double)(i - (w - tailEnd)) / Math.Max(tailEnd - 1, 1) : 0;
                    wiggle = isTail ? Math.Sin(timeFactor + t * 2) * t * 12 : 0;
                    srcCol = w - i - 1;
                    dstBaseX = margin + i - (int)Math.Round(wiggle);
                }

                if (dstBaseX < 0 || dstBaseX >= _wiggleWidth) continue;

                for (int row = 0; row < h; row++)
                {
                    int srcIdx = (row * w + srcCol) * 4;
                    int dstIdx = (row * _wiggleWidth + dstBaseX) * 4;
                    _wiggleBuffer[dstIdx] = _pixelData[srcIdx];
                    _wiggleBuffer[dstIdx + 1] = _pixelData[srcIdx + 1];
                    _wiggleBuffer[dstIdx + 2] = _pixelData[srcIdx + 2];
                    _wiggleBuffer[dstIdx + 3] = _pixelData[srcIdx + 3];
                }
            }

            var bmp = new WriteableBitmap(_wiggleWidth, _wiggleHeight, 96, 96, PixelFormats.Bgra32, null);
            bmp.WritePixels(new Int32Rect(0, 0, _wiggleWidth, _wiggleHeight),
                            _wiggleBuffer, _wiggleWidth * 4, 0);
            bmp.Freeze();
            return bmp;
        }

        public void UpdatePhysics(double canvasWidth, double canvasHeight)
        {
            if (IsDying || IsEntering) return;

            Vx += Speed * Direction * 0.1;
            X += Vx;
            Y += Vy;

            bool hitEdge = false;
            if (X <= 0)
            {
                X = 0; Direction = 1; Vx = Math.Abs(Vx); hitEdge = true;
            }
            else if (X >= canvasWidth - Width)
            {
                X = canvasWidth - Width; Direction = -1; Vx = -Math.Abs(Vx); hitEdge = true;
            }
            if (Y <= 0)
            {
                Y = 0; Vy = Math.Abs(Vy) * 0.5; hitEdge = true;
            }
            else if (Y >= canvasHeight - Height)
            {
                Y = canvasHeight - Height; Vy = -Math.Abs(Vy) * 0.5; hitEdge = true;
            }

            Vx *= 0.85;
            Vy *= 0.85;

            double maxVel = Speed * 2;
            double velMag = Math.Sqrt(Vx * Vx + Vy * Vy);
            if (velMag > maxVel)
            {
                Vx = (Vx / velMag) * maxVel;
                Vy = (Vy / velMag) * maxVel;
            }
            if (Math.Abs(Vx) < 0.1) Vx = Speed * Direction * 0.1;
            if (hitEdge)
            {
                Vx += Speed * Direction * 0.2;
                Vy += (Rng.NextDouble() - 0.5) * 0.3; // -0.15..0.15
            }
        }

        public void UpdateEntrance(double currentTime)
        {
            if (!IsEntering) return;
            double elapsed = currentTime - _enterStartTime;
            double progress = Math.Min(elapsed / 1.0, 1.0);
            Opacity = progress;
            Scale = 0.3 + progress * 0.7;
            if (progress >= 1.0)
            {
                IsEntering = false;
                Opacity = 1.0;
                Scale = 1.0;
            }
        }

        public void UpdateDeath(double currentTime)
        {
            if (!IsDying) return;
            double elapsed = currentTime - _deathStartTime;
            double progress = Math.Min(elapsed / 2.0, 1.0);
            Opacity = 1.0 - progress;
            Y = _originalY + progress * progress * 200;
        }

        public bool IsDeathComplete(double currentTime)
        {
            return IsDying && currentTime - _deathStartTime >= 2.0;
        }

        public void StartEntrance(double currentTime)
        {
            IsEntering = true;
            _enterStartTime = currentTime;
            Opacity = 0;
            Scale = 0.3;
        }

        public void StartDeath(double currentTime)
        {
            IsDying = true;
            _deathStartTime = currentTime;
            _originalY = Y;
        }

        public void Draw(DrawingContext dc, double time)
        {
            if (_pixelWidth <= 0 || _pixelHeight <= 0) return;
            var frame = BuildWiggleFrame(time);
            if (frame == null) return;

            // WPF Y is top-down (0 = top), matching the macOS screensaver's coordinate usage
            double swimY = IsDying ? Y : Y + Math.Sin(time + Phase) * Amplitude;
            double margin = (_wiggleWidth - _pixelWidth) / 2.0;

            dc.PushOpacity(Opacity);

            if (IsEntering && Scale != 1.0)
            {
                double cx = X + Width / 2;
                double cy = swimY + Height / 2;
                dc.PushTransform(new TranslateTransform(cx, cy));
                dc.PushTransform(new ScaleTransform(Scale, Scale));
                dc.PushTransform(new TranslateTransform(-(Width / 2 + margin), -Height / 2));
                dc.DrawImage(frame, new Rect(0, 0, _wiggleWidth, _wiggleHeight));
                dc.Pop(); // translate
                dc.Pop(); // scale
                dc.Pop(); // translate
            }
            else
            {
                dc.DrawImage(frame, new Rect(X - margin, swimY, _wiggleWidth, _wiggleHeight));
            }

            dc.Pop(); // opacity
        }
    }
}

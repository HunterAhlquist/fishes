using System;
using System.IO;

namespace FishTankScreensaver
{
    public static class FishLog
    {
        private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "fishtank_screensaver_win.log");

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:O}] {message}\n");
            }
            catch { }
        }
    }
}

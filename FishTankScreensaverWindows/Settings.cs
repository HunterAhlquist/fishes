using System;
using System.IO;
using System.Text.Json;

namespace FishTankScreensaver
{
    public class ScreensaverSettings
    {
        public string BackgroundColor { get; set; } = "e0f7fa";
        public int FishCount { get; set; } = 50;
        public string SortType { get; set; } = "recent"; // recent, popular, random

        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FishTankScreensaver");

        private static readonly string ConfigPath = Path.Combine(ConfigDir, "settings.json");

        public static ScreensaverSettings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<ScreensaverSettings>(json) ?? new ScreensaverSettings();
                }
            }
            catch { }
            return new ScreensaverSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }

        public string BuildTankUrl()
        {
            return $"https://drawafish.com/tank.html?mode=screensaver&bg={BackgroundColor}&sort={SortType}&capacity={FishCount}";
        }
    }
}

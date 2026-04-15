using System;
using System.IO;
using System.Windows;

namespace FishTankScreensaver
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Use the raw command line — WPF's e.Args can mangle screensaver arguments.
            // Windows passes: FishTankScreensaver.scr /s
            //                 FishTankScreensaver.scr /c:HWND
            //                 FishTankScreensaver.scr /p HWND
            var cmdLine = Environment.CommandLine.ToLower();

            // Log for debugging
            var logPath = Path.Combine(Path.GetTempPath(), "fishtank_screensaver_win.log");
            try { File.AppendAllText(logPath, $"[{DateTime.Now:O}] cmdLine: {cmdLine}\n"); } catch { }

            if (cmdLine.Contains("/s"))
            {
                var window = new ScreensaverWindow();
                window.Show();
            }
            else if (cmdLine.Contains("/p"))
            {
                // Preview mode — show a small standalone preview window
                var window = new ScreensaverWindow(isPreview: true);
                window.Show();
            }
            else
            {
                // Default: configure mode (/c or no args)
                var window = new SettingsWindow();
                window.Show();
            }
        }
    }
}

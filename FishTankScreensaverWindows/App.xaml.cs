using System;
using System.Linq;
using System.Windows;

namespace FishTankScreensaver
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Windows screensaver args come in various formats:
            //   /s           - run screensaver
            //   /c           - configure (no parent)
            //   /c:HWND      - configure (with parent window handle)
            //   /p HWND      - preview in small window
            //   /p:HWND      - preview in small window (alternate format)
            // Args may also be split across multiple tokens

            var cmdLine = string.Join(" ", e.Args).ToLower().Trim();

            if (string.IsNullOrEmpty(cmdLine) || cmdLine.StartsWith("/c") || cmdLine.StartsWith("-c"))
            {
                // Configure mode (also default when no args)
                var window = new SettingsWindow();
                window.Show();
            }
            else if (cmdLine.StartsWith("/s") || cmdLine.StartsWith("-s"))
            {
                // Full screensaver mode
                var window = new ScreensaverWindow();
                window.Show();
            }
            else if (cmdLine.StartsWith("/p") || cmdLine.StartsWith("-p"))
            {
                // Preview mode - show a small preview window
                var window = new ScreensaverWindow(isPreview: true);
                window.Show();
            }
            else
            {
                // Unknown args, show settings
                var window = new SettingsWindow();
                window.Show();
            }
        }
    }
}

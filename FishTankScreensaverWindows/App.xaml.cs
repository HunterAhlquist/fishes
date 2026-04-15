using System;
using System.Windows;

namespace FishTankScreensaver
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var args = e.Args;
            string mode = "c"; // Default to configure

            if (args.Length > 0)
            {
                var arg = args[0].ToLower().TrimStart('/').TrimStart('-');

                if (arg == "s")
                {
                    mode = "s";
                }
                else if (arg.StartsWith("p"))
                {
                    // Preview mode - just exit since preview in the tiny
                    // Settings window is not practical with WebView2
                    Shutdown();
                    return;
                }
                else if (arg.StartsWith("c"))
                {
                    mode = "c";
                }
            }

            if (mode == "s")
            {
                var window = new ScreensaverWindow();
                window.Show();
            }
            else
            {
                var window = new SettingsWindow();
                window.ShowDialog();
                Shutdown();
            }
        }
    }
}

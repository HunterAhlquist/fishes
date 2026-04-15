using System;
using System.IO;
using System.Windows;

namespace FishTankScreensaver
{
    public partial class App : Application
    {
        private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "fishtank_screensaver_win.log");

        private static void Log(string message)
        {
            try { File.AppendAllText(LogPath, $"[{DateTime.Now:O}] {message}\n"); } catch { }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Catch all unhandled exceptions so we can log them
            DispatcherUnhandledException += (s, ex) =>
            {
                Log($"UNHANDLED: {ex.Exception}");
                ex.Handled = true;
                Shutdown(1);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                Log($"DOMAIN UNHANDLED: {ex.ExceptionObject}");
            };

            try
            {
                // Parse args from e.Args to avoid matching substrings in the exe path
                var mode = "c"; // default to configure
                foreach (var arg in e.Args)
                {
                    var a = arg.ToLower().Trim();
                    Log($"arg: [{a}]");

                    if (a == "/s" || a == "-s")
                        mode = "s";
                    else if (a.StartsWith("/c") || a.StartsWith("-c"))
                        mode = "c";
                    else if (a.StartsWith("/p") || a.StartsWith("-p"))
                        mode = "p";
                }

                Log($"mode: {mode}");

                switch (mode)
                {
                    case "s":
                        Log("Launching screensaver");
                        var ss = new ScreensaverWindow();
                        ss.Show();
                        break;

                    case "p":
                        // Preview: Windows wants us to render inside a tiny HWND.
                        // WebView2 can't easily do that, so just exit cleanly.
                        Log("Preview requested, exiting");
                        Shutdown();
                        break;

                    default:
                        Log("Launching settings");
                        var sw = new SettingsWindow();
                        sw.Closed += (s2, e2) => Shutdown();
                        sw.Show();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"STARTUP ERROR: {ex}");
                MessageBox.Show($"Error starting screensaver:\n{ex.Message}", "Fish Tank Screensaver", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }
    }
}

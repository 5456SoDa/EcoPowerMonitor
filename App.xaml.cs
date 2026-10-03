using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;

namespace EcoPowerMonitor
{
    public partial class App : System.Windows.Application
    {
        private static System.Threading.Mutex? _singleInstanceMutex;
        private static System.Threading.EventWaitHandle? _restoreEvent;
        private const string MutexName = "Global\\EcoPowerMonitor_SingleInstance_Mutex";
        private const string EventName = "Global\\EcoPowerMonitor_RestoreEvent";

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetDefaultDllDirectories(uint directoryFlags);
        private const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Harden application against DLL search-order hijacking / Binary Planting
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
                }
                catch { }
            }

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                LogFatalException("AppDomain.UnhandledException", args.ExceptionObject as Exception);
            };

            DispatcherUnhandledException += (s, args) =>
            {
                LogFatalException("DispatcherUnhandledException", args.Exception);
                args.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                LogFatalException("TaskScheduler.UnobservedTaskException", args.Exception);
                args.SetObserved();
            };

            // 1. Single Instance Check: Prevent multiple processes and duplicate tray icons
            bool isNewInstance;
            try
            {
                _singleInstanceMutex = new System.Threading.Mutex(true, MutexName, out isNewInstance);
            }
            catch
            {
                isNewInstance = true;
            }

            if (!isNewInstance)
            {
                // Another instance is already running -> signal it to restore its window and exit
                try
                {
                    using var ev = System.Threading.EventWaitHandle.OpenExisting(EventName);
                    ev.Set();
                }
                catch { }

                Shutdown();
                return;
            }

            // Start background listener to restore window if user launches another instance
            try
            {
                _restoreEvent = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, EventName);
                Task.Run(() =>
                {
                    while (_restoreEvent != null)
                    {
                        try
                        {
                            if (_restoreEvent.WaitOne())
                            {
                                Dispatcher.InvokeAsync(() =>
                                {
                                    if (MainWindow is MainWindow mw)
                                    {
                                        mw.Show();
                                        if (mw.WindowState == WindowState.Minimized)
                                        {
                                            mw.WindowState = WindowState.Normal;
                                        }
                                        mw.Activate();
                                        mw.Focus();
                                    }
                                });
                            }
                        }
                        catch { break; }
                    }
                });
            }
            catch { }

            // 2. Auto-elevate to Administrator at launch if running without elevation on Windows
            if (OperatingSystem.IsWindows() && !IsAdministrator())
            {
                try
                {
                    var exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = exePath,
                            UseShellExecute = true,
                            Verb = "runas"
                        };

                        // Release single-instance mutex and event before spawning elevated process
                        _singleInstanceMutex?.ReleaseMutex();
                        _singleInstanceMutex?.Dispose();
                        _singleInstanceMutex = null;
                        _restoreEvent?.Dispose();
                        _restoreEvent = null;

                        System.Diagnostics.Process.Start(psi);
                        Shutdown();
                        return;
                    }
                }
                catch
                {
                    // User declined UAC elevation prompt, continue running with available sensors
                }
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _singleInstanceMutex?.ReleaseMutex();
                _singleInstanceMutex?.Dispose();
                _singleInstanceMutex = null;
                _restoreEvent?.Dispose();
                _restoreEvent = null;
            }
            catch { }
            base.OnExit(e);
        }

        private static bool IsAdministrator()
        {
            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static void LogFatalException(string source, Exception? ex)
        {
            string msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FATAL ({source}): {ex?.ToString()}";
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.log");
                File.AppendAllText(path, msg + Environment.NewLine);
                MessageBox.Show(
                    $"พบข้อผิดพลาด:\n{ex?.Message}\n\nบันทึกรายละเอียดไว้ที่:\n{path}",
                    "EcoPower Monitor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch { }
        }

        public static void ApplyTheme(string themeName)
        {
            if (Current == null) return;

            var res = Current.Resources;
            if (themeName.Equals("Light", StringComparison.OrdinalIgnoreCase))
            {
                // Light Porcelain & Ergonomic Slate Palette
                res["BgBrush"] = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                res["CardBgBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                res["CardBorderBrush"] = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                res["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                res["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                res["AccentBrush"] = new SolidColorBrush(Color.FromRgb(2, 132, 199));
                res["AccentSubtleBrush"] = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                res["AccentHighlightBrush"] = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                res["ButtonBorderBrush"] = new SolidColorBrush(Color.FromArgb(35, 15, 23, 42));

                // Vivid Electric Blue Telemetry Curve in Light Mode (Not harsh black!)
                res["GraphLineBrush"] = new SolidColorBrush(Color.FromRgb(2, 132, 199));
                res["GraphFillBrush"] = new SolidColorBrush(Color.FromArgb(25, 2, 132, 199));
                res["GraphGridBrush"] = new SolidColorBrush(Color.FromArgb(20, 15, 23, 42));

                res["AlertBgBrush"] = new SolidColorBrush(Color.FromRgb(254, 242, 242));
                res["AlertBorderBrush"] = new SolidColorBrush(Color.FromRgb(254, 202, 202));
                res["AlertTextBrush"] = new SolidColorBrush(Color.FromRgb(185, 28, 28));

                res["ScrollBarThumbBrush"] = new SolidColorBrush(Color.FromRgb(203, 213, 225));
                res["ScrollBarThumbHoverBrush"] = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                res["ScrollBarThumbDragBrush"] = new SolidColorBrush(Color.FromRgb(2, 132, 199));

                res["GraphAreaGradientBrush"] = new LinearGradientBrush(
                    Color.FromArgb(45, 2, 132, 199),
                    Color.FromArgb(0, 2, 132, 199),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(0, 1));
            }
            else
            {
                // Obsidian Deep Slate Palette (Comfortable, Zero Glare)
                res["BgBrush"] = new SolidColorBrush(Color.FromRgb(11, 15, 25));
                res["CardBgBrush"] = new SolidColorBrush(Color.FromRgb(17, 24, 39));
                res["CardBorderBrush"] = new SolidColorBrush(Color.FromRgb(31, 41, 55));
                res["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(249, 250, 251));
                res["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                res["AccentBrush"] = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                res["AccentSubtleBrush"] = new SolidColorBrush(Color.FromRgb(22, 31, 48));
                res["AccentHighlightBrush"] = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                res["ButtonBorderBrush"] = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));

                res["GraphLineBrush"] = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                res["GraphFillBrush"] = new SolidColorBrush(Color.FromArgb(24, 56, 189, 248));
                res["GraphGridBrush"] = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));

                res["AlertBgBrush"] = new SolidColorBrush(Color.FromRgb(56, 30, 30));
                res["AlertBorderBrush"] = new SolidColorBrush(Color.FromRgb(96, 40, 40));
                res["AlertTextBrush"] = new SolidColorBrush(Color.FromRgb(255, 160, 160));

                res["ScrollBarThumbBrush"] = new SolidColorBrush(Color.FromRgb(51, 65, 85));
                res["ScrollBarThumbHoverBrush"] = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                res["ScrollBarThumbDragBrush"] = new SolidColorBrush(Color.FromRgb(56, 189, 248));

                res["GraphAreaGradientBrush"] = new LinearGradientBrush(
                    Color.FromArgb(60, 56, 189, 248),
                    Color.FromArgb(0, 56, 189, 248),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(0, 1));
            }
        }
    }
}

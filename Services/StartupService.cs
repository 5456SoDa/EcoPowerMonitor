using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace EcoPowerMonitor.Services
{
    public static class StartupService
    {
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "EcoPowerMonitor";
        private const string TaskName = "EcoPowerMonitorStartup";

        /// <summary>
        /// Checks whether the application is registered to run on Windows startup (via Task Scheduler or Registry).
        /// </summary>
        public static bool IsRunOnStartupEnabled()
        {
            if (OperatingSystem.IsWindows())
            {
                // 1. Check Task Scheduler (preferred for elevated apps)
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "schtasks.exe",
                        Arguments = $"/query /tn \"{TaskName}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        proc.WaitForExit(1500);
                        if (proc.ExitCode == 0) return true;
                    }
                }
                catch { }

                // 2. Fallback check Registry
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, writable: false);
                    if (key != null)
                    {
                        var value = key.GetValue(AppName) as string;
                        if (!string.IsNullOrWhiteSpace(value)) return true;
                    }
                }
                catch { }
            }

            return false;
        }

        /// <summary>
        /// Configures the application to run (or not run) on Windows startup with highest privileges.
        /// </summary>
        public static bool SetRunOnStartup(bool enable)
        {
            if (!OperatingSystem.IsWindows()) return false;

            string? exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath))
            {
                exePath = Process.GetCurrentProcess().MainModule?.FileName;
            }

            if (enable)
            {
                if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath)) return false;

                bool taskSuccess = false;
                // 1. Register Task Scheduler with /rl highest (runs as admin at logon without UAC prompt)
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "schtasks.exe",
                        Arguments = $"/create /tn \"{TaskName}\" /tr \"\\\"{exePath}\\\"\" /sc onlogon /rl highest /f",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        proc.WaitForExit(3000);
                        taskSuccess = proc.ExitCode == 0;
                    }
                }
                catch { }

                // 2. Also set Registry key as standard backup
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, writable: true);
                    if (key != null)
                    {
                        key.SetValue(AppName, $"\"{exePath}\"");
                    }
                }
                catch { }

                return taskSuccess;
            }
            else
            {
                // 1. Delete Scheduled Task
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "schtasks.exe",
                        Arguments = $"/delete /tn \"{TaskName}\" /f",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        proc.WaitForExit(3000);
                    }
                }
                catch { }

                // 2. Remove Registry key
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, writable: true);
                    if (key?.GetValue(AppName) != null)
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
                catch { }

                return true;
            }
        }
    }
}

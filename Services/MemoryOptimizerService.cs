using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EcoPowerMonitor.Services
{
    public static class MemoryOptimizerService
    {
        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hwProc);

        /// <summary>
        /// Trims unused memory and frees working set memory back to Windows operating system.
        /// Ideal when minimizing to Taskbar or System Tray.
        /// </summary>
        public static void TrimWorkingSet()
        {
            try
            {
                // Request GC collection on background generation
                GC.Collect(2, GCCollectionMode.Optimized, false);
                GC.WaitForPendingFinalizers();

                // Call Windows API to trim physical memory working set
                using var currentProcess = Process.GetCurrentProcess();
                EmptyWorkingSet(currentProcess.Handle);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to trim working set memory: {ex.Message}");
            }
        }
    }
}

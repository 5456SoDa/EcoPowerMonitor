using System;

namespace EcoPowerMonitor.Services
{
    public static class HardwareMonitorFactory
    {
        public static IHardwareMonitorService Create()
        {
            if (OperatingSystem.IsLinux())
            {
                return new LinuxHardwareMonitorService();
            }
            return new HardwareMonitorService();
        }
    }
}

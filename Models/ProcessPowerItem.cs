using System;

namespace EcoPowerMonitor.Models
{
    public class ProcessPowerItem
    {
        public int Rank { get; set; }
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public double CpuPercentage { get; set; }
        public double EstimatedWatts { get; set; }
        public double MemoryMB { get; set; }
        public string FormattedWatts => $"{EstimatedWatts:F1} W";
        public string FormattedCpu => $"{CpuPercentage:F1}%";
        public string FormattedMemory => $"{MemoryMB:F0} MB";
        public string FormattedCostPerHour { get; set; } = string.Empty;
    }
}

using System;

namespace EcoPowerMonitor.Models
{
    public class DailyPowerRecord
    {
        public string DateString { get; set; } = string.Empty; // YYYY-MM-DD
        public double TotalKWh { get; set; }
        public double TotalCost { get; set; }
        public double PeakWatts { get; set; }
        public double AverageWatts { get; set; }
        public int ActiveMinutes { get; set; }
        public string CurrencySymbol { get; set; } = "฿";

        public string FormattedKWh => $"{TotalKWh:F3} kWh";
        public string FormattedCost => $"{TotalCost:F2} {CurrencySymbol}";
        public string FormattedPeak => $"{PeakWatts:F1} W";
        public string FormattedAvg => $"{AverageWatts:F1} W";
        public string FormattedDuration => $"{ActiveMinutes / 60}h {ActiveMinutes % 60}m";
    }
}

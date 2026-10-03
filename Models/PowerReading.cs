using System;

namespace EcoPowerMonitor.Models
{
    public class PowerReading
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Total System Estimated / Measured Power
        public double TotalWatts { get; set; }

        // CPU
        public string CpuName { get; set; } = "CPU";
        public double CpuPackageWatts { get; set; }
        public double CpuCoresWatts { get; set; }
        public double CpuTemperature { get; set; }
        public double CpuLoadPercentage { get; set; }

        // GPU
        public string GpuName { get; set; } = "GPU";
        public double GpuPowerWatts { get; set; }
        public double GpuTemperature { get; set; }
        public double GpuLoadPercentage { get; set; }

        // Battery (for Laptops)
        public bool HasBattery { get; set; }
        public bool IsAcOnline { get; set; } = true;
        public bool IsCharging { get; set; }
        public double BatteryRateWatts { get; set; } // Discharge or charge rate
        public double BatteryPercentage { get; set; }
        public double? BatteryRemainingMinutes { get; set; }
        public double BatteryCapacityWh { get; set; }
        public double BatteryDesignCapacityWh { get; set; }
        public double BatteryWearLevelPercentage { get; set; }

        // Formatted Battery Presentation (Clean, rock-solid, zero glitch)
        public string BatteryChargeStatusText
        {
            get
            {
                if (!HasBattery) return string.Empty;
                if (IsAcOnline)
                {
                    if (BatteryPercentage >= 99)
                        return "(เต็ม 100% / AC Connected)";
                    if (IsCharging)
                        return "(กำลังชาร์จ / Charging)";
                    return "(เสียบสายชาร์จ / Plugged In)";
                }
                return "(ใช้แบตเตอรี่ / Discharging)";
            }
        }

        public string FormattedBatteryRemaining
        {
            get
            {
                if (!HasBattery) return "N/A";
                if (IsAcOnline)
                {
                    if (BatteryPercentage >= 99)
                        return "AC Mains (100%)";
                    if (IsCharging)
                        return BatteryRemainingMinutes.HasValue && BatteryRemainingMinutes.Value > 0
                            ? $"{Math.Round(BatteryRemainingMinutes.Value)} min"
                            : "กำลังชาร์จ (Charging)";
                    return "AC Connected";
                }

                if (BatteryRemainingMinutes.HasValue && BatteryRemainingMinutes.Value > 0)
                {
                    int totalMin = (int)Math.Round(BatteryRemainingMinutes.Value);
                    int hours = totalMin / 60;
                    int mins = totalMin % 60;
                    if (hours > 0)
                        return $"{hours}h {mins}m";
                    return $"{mins} min";
                }

                return "กำลังคำนวณ...";
            }
        }

        // Real-time Cost
        public double CostPerHour { get; set; }
        public double CostPerDay { get; set; }
        public double CostPerMonth { get; set; }

        // Alert Flags
        public bool IsOverpower { get; set; }
        public bool IsHighTemp { get; set; }
        public bool HasAlert => IsOverpower || IsHighTemp;
        public string? AlertMessage { get; set; }

        // Time-of-Use (TOU) Telemetry
        public bool IsTouActive { get; set; }
        public bool IsTouOnPeak { get; set; }
        public double ActiveTariffRate { get; set; }

        // Carbon Footprint Telemetry (CO2e)
        public double CarbonPerHourKg { get; set; }
        public double AccumulatedCarbonKg { get; set; }
    }
}

using System;
using EcoPowerMonitor.Models;

namespace EcoPowerMonitor.Services
{
    public class CostCalculatorService
    {
        private DateTime _lastCalculationTime = DateTime.MinValue;
        private readonly PowerStatistics _statistics = new();
        private readonly object _lock = new();

        public PowerStatistics Statistics
        {
            get
            {
                lock (_lock)
                {
                    return _statistics;
                }
            }
        }

        public void ProcessReading(PowerReading reading, AppSettings settings)
        {
            lock (_lock)
            {
                DateTime now = DateTime.Now;
                double rate = Math.Max(0.01, settings.ElectricityRatePerKWh);

                if (settings.UseTouTariff)
                {
                    bool isWeekend = now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;
                    bool isOnPeakTime = now.Hour >= 9 && now.Hour < 22;
                    bool isOnPeak = !isWeekend && isOnPeakTime;
                    rate = isOnPeak ? Math.Max(0.01, settings.TouOnPeakRate) : Math.Max(0.01, settings.TouOffPeakRate);
                    reading.IsTouActive = true;
                    reading.IsTouOnPeak = isOnPeak;
                    reading.ActiveTariffRate = rate;
                }
                else
                {
                    reading.IsTouActive = false;
                    reading.ActiveTariffRate = rate;
                }

                // Real-time cost rate
                double kw = reading.TotalWatts / 1000.0;
                reading.CostPerHour = kw * rate;
                reading.CostPerDay = reading.CostPerHour * 24.0;
                reading.CostPerMonth = reading.CostPerHour * 24.0 * 30.0;

                // Carbon footprint calculation (CO2e)
                double carbonFactor = settings.CarbonEmissionFactor > 0 ? settings.CarbonEmissionFactor : 0.4999;
                reading.CarbonPerHourKg = kw * carbonFactor;
                reading.AccumulatedCarbonKg = _statistics.AccumulatedKWh * carbonFactor;

                // Alerts evaluation
                if (reading.TotalWatts >= settings.OverpowerThresholdWatts)
                {
                    reading.IsOverpower = true;
                    reading.AlertMessage = settings.Language == "th"
                        ? $"แจ้งเตือน: การใช้ไฟสูงผิดปกติ ({reading.TotalWatts:F1} W เกินขีดจำกัด {settings.OverpowerThresholdWatts:F0} W)"
                        : $"Warning: High Power Draw ({reading.TotalWatts:F1} W exceeds threshold {settings.OverpowerThresholdWatts:F0} W)";
                }
                else if (reading.CpuTemperature >= settings.HighTempThresholdCelsius || reading.GpuTemperature >= settings.HighTempThresholdCelsius)
                {
                    reading.IsHighTemp = true;
                    double maxTemp = Math.Max(reading.CpuTemperature, reading.GpuTemperature);
                    reading.AlertMessage = settings.Language == "th"
                        ? $"แจ้งเตือน: อุณหภูมิฮาร์ดแวร์สูง ({maxTemp:F1} °C เกินขีดจำกัด {settings.HighTempThresholdCelsius:F0} °C)"
                        : $"Warning: High Hardware Temperature ({maxTemp:F1} °C exceeds threshold {settings.HighTempThresholdCelsius:F0} °C)";
                }
                else
                {
                    reading.IsOverpower = false;
                    reading.IsHighTemp = false;
                    reading.AlertMessage = null;
                }

                // Update session statistics
                _statistics.CurrentWatts = reading.TotalWatts;
                _statistics.TotalReadingsCount++;

                if (reading.TotalWatts > _statistics.PeakWatts)
                {
                    _statistics.PeakWatts = reading.TotalWatts;
                }

                if (reading.TotalWatts > 0 && (reading.TotalWatts < _statistics.MinWatts || _statistics.MinWatts == double.MaxValue))
                {
                    _statistics.MinWatts = reading.TotalWatts;
                }

                if (_lastCalculationTime != DateTime.MinValue)
                {
                    double deltaHours = (now - _lastCalculationTime).TotalHours;
                    // Protect against clock adjustments or long sleeps
                    if (deltaHours > 0 && deltaHours < 1.0)
                    {
                        double wattHours = reading.TotalWatts * deltaHours;
                        _statistics.AccumulatedWattHours += wattHours;
                        _statistics.AccumulatedCost += (wattHours / 1000.0) * rate;
                    }
                }
                _lastCalculationTime = now;

                // Running average calculation
                double totalHours = _statistics.SessionDuration.TotalHours;
                if (totalHours > 0 && _statistics.AccumulatedWattHours > 0)
                {
                    _statistics.AverageWatts = _statistics.AccumulatedWattHours / totalHours;
                }
                else
                {
                    _statistics.AverageWatts = reading.TotalWatts;
                }
            }
        }

        public void ResetSession()
        {
            lock (_lock)
            {
                _statistics.Reset();
                _lastCalculationTime = DateTime.MinValue;
            }
        }
    }
}

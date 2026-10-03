using System;

namespace EcoPowerMonitor.Models
{
    public class PowerStatistics
    {
        public double CurrentWatts { get; set; }
        public double AverageWatts { get; set; }
        public double PeakWatts { get; set; }
        private double _minWatts = double.MaxValue;
        public double MinWatts
        {
            get => _minWatts == double.MaxValue ? 0.0 : _minWatts;
            set => _minWatts = value;
        }

        public double AccumulatedWattHours { get; set; }
        public double AccumulatedKWh => AccumulatedWattHours / 1000.0;
        public double AccumulatedCost { get; set; }

        public DateTime SessionStartTime { get; set; } = DateTime.Now;
        public TimeSpan SessionDuration => DateTime.Now - SessionStartTime;
        public long TotalReadingsCount { get; set; }

        public void Reset()
        {
            CurrentWatts = 0;
            AverageWatts = 0;
            PeakWatts = 0;
            _minWatts = double.MaxValue;
            AccumulatedWattHours = 0;
            AccumulatedCost = 0;
            SessionStartTime = DateTime.Now;
            TotalReadingsCount = 0;
        }
    }
}

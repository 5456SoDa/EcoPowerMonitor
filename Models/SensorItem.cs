using EcoPowerMonitor.ViewModels;

namespace EcoPowerMonitor.Models
{
    public class SensorItem : ViewModelBase
    {
        private double _value;
        private double _minValue;
        private double _maxValue;

        public string HardwareType { get; set; } = string.Empty;
        public string HardwareName { get; set; } = string.Empty;
        public string SensorName { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        public double Value
        {
            get => _value;
            set
            {
                if (SetField(ref _value, value))
                {
                    OnPropertyChanged(nameof(FormattedValue));
                }
            }
        }

        public double MinValue
        {
            get => _minValue;
            set
            {
                if (SetField(ref _minValue, value))
                {
                    OnPropertyChanged(nameof(FormattedMin));
                }
            }
        }

        public double MaxValue
        {
            get => _maxValue;
            set
            {
                if (SetField(ref _maxValue, value))
                {
                    OnPropertyChanged(nameof(FormattedMax));
                }
            }
        }

        public string FormattedValue => $"{Value:F1} {Unit}";
        public string FormattedMin => $"{MinValue:F1} {Unit}";
        public string FormattedMax => $"{MaxValue:F1} {Unit}";

        public void UpdateValues(double val, double min, double max)
        {
            if (val != _value)
            {
                _value = val;
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(FormattedValue));
            }

            if (min != _minValue)
            {
                _minValue = min;
                OnPropertyChanged(nameof(MinValue));
                OnPropertyChanged(nameof(FormattedMin));
            }

            if (max != _maxValue)
            {
                _maxValue = max;
                OnPropertyChanged(nameof(MaxValue));
                OnPropertyChanged(nameof(FormattedMax));
            }
        }
    }
}

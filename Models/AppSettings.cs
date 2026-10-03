using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EcoPowerMonitor.Models
{
    public class AppSettings : INotifyPropertyChanged
    {
        private double _electricityRatePerKWh = 4.50;
        private string _currencySymbol = "฿";
        private string _currencyCode = "THB";
        private string _language = "th";
        private string _theme = "Dark";
        private int _pollingRateMs = 1000;
        private double _overpowerThresholdWatts = 350.0;
        private double _highTempThresholdCelsius = 85.0;
        private bool _startWithWindows = false;
        private bool _minimizeToTray = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public double ElectricityRatePerKWh
        {
            get => _electricityRatePerKWh;
            set => SetField(ref _electricityRatePerKWh, value);
        }

        public string CurrencySymbol
        {
            get => _currencySymbol;
            set => SetField(ref _currencySymbol, value);
        }

        public string CurrencyCode
        {
            get => _currencyCode;
            set => SetField(ref _currencyCode, value);
        }

        public string Language
        {
            get => _language;
            set => SetField(ref _language, value);
        }

        public string Theme
        {
            get => _theme;
            set => SetField(ref _theme, value);
        }

        public int PollingRateMs
        {
            get => _pollingRateMs;
            set => SetField(ref _pollingRateMs, value);
        }

        public double OverpowerThresholdWatts
        {
            get => _overpowerThresholdWatts;
            set => SetField(ref _overpowerThresholdWatts, value);
        }

        public double HighTempThresholdCelsius
        {
            get => _highTempThresholdCelsius;
            set => SetField(ref _highTempThresholdCelsius, value);
        }

        public bool StartWithWindows
        {
            get => _startWithWindows;
            set => SetField(ref _startWithWindows, value);
        }

        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            set => SetField(ref _minimizeToTray, value);
        }

        // Time-of-Use (TOU) Tariff Settings
        private bool _useTouTariff = false;
        public bool UseTouTariff
        {
            get => _useTouTariff;
            set => SetField(ref _useTouTariff, value);
        }

        private double _touOnPeakRate = 5.80;
        public double TouOnPeakRate
        {
            get => _touOnPeakRate;
            set => SetField(ref _touOnPeakRate, value);
        }

        private double _touOffPeakRate = 2.65;
        public double TouOffPeakRate
        {
            get => _touOffPeakRate;
            set => SetField(ref _touOffPeakRate, value);
        }

        // Carbon Footprint Emission Factor (kg CO2e / kWh)
        private double _carbonEmissionFactor = 0.4999;
        public double CarbonEmissionFactor
        {
            get => _carbonEmissionFactor;
            set => SetField(ref _carbonEmissionFactor, value);
        }
    }
}

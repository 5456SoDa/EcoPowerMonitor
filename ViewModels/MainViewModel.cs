using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EcoPowerMonitor.Models;
using EcoPowerMonitor.Services;
using Microsoft.Win32;
using System.Windows.Shell;
using Point = System.Windows.Point;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace EcoPowerMonitor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IHardwareMonitorService _monitorService;
        private readonly CostCalculatorService _costCalculator;
        private readonly DatabaseService _databaseService;
        private readonly LocalizationService _loc;
        private readonly Dispatcher _dispatcher;
        private readonly IProcessMonitorService _processMonitorService;
        private readonly DispatcherTimer _smoothCounterTimer;

        private PowerReading _currentReading = new();
        private AppSettings _settings = new();
        private int _selectedTabIndex = 0;
        private string _searchFilter = string.Empty;
        private string? _statusNotification;
        private bool _isNotificationVisible;
        private DateTime _lastDbSave = DateTime.Now;
        private double _lastSavedKWh = 0.0;
        private double _lastSavedCost = 0.0;
        private readonly Queue<double> _livePowerQueue = new();
        private double _displayedTotalWatts = 0.0;

        public PowerReading CurrentReading
        {
            get => _currentReading;
            set => SetField(ref _currentReading, value);
        }

        public double DisplayedTotalWatts
        {
            get => _displayedTotalWatts;
            private set => SetField(ref _displayedTotalWatts, value);
        }

        public string FormattedDisplayedWatts => $"{_displayedTotalWatts:F1}";

        public ObservableCollection<ProcessPowerItem> TopPowerProcesses { get; } = new();

        private bool _isWindowVisible = true;
        public bool IsWindowVisible
        {
            get => _isWindowVisible;
            set
            {
                if (SetField(ref _isWindowVisible, value))
                {
                    UpdateActiveTelemetryState();
                }
            }
        }

        private bool _isMiniHudVisible = false;
        public bool IsMiniHudVisible
        {
            get => _isMiniHudVisible;
            set
            {
                if (SetField(ref _isMiniHudVisible, value))
                {
                    UpdateActiveTelemetryState();
                }
            }
        }

        private void UpdateActiveTelemetryState()
        {
            bool isAnyUiActive = _isWindowVisible || _isMiniHudVisible;
            if (isAnyUiActive)
            {
                _monitorService.UpdatePollingRate(Settings.PollingRateMs);
                if (!_smoothCounterTimer.IsEnabled)
                {
                    _smoothCounterTimer.Start();
                }
                if (_isWindowVisible)
                {
                    RenderLiveGraph();
                    ApplySensorFilter();
                }
            }
            else
            {
                _monitorService.UpdatePollingRate(Math.Max(Settings.PollingRateMs, 2000));
                _smoothCounterTimer.Stop();
                MemoryOptimizerService.TrimWorkingSet();
            }
        }

        public AppSettings Settings
        {
            get => _settings;
            set => SetField(ref _settings, value);
        }

        public PowerStatistics Statistics => _costCalculator.Statistics;

        public ObservableCollection<SensorItem> DetailedSensors { get; } = new();
        public ObservableCollection<SensorItem> FilteredSensors { get; } = new();
        public ObservableCollection<DailyPowerRecord> DailyRecords { get; } = new();

        private PointCollection _liveGraphPoints = new();
        public PointCollection LiveGraphPoints
        {
            get => _liveGraphPoints;
            private set => SetField(ref _liveGraphPoints, value);
        }

        private PointCollection _liveGraphAreaPoints = new();
        public PointCollection LiveGraphAreaPoints
        {
            get => _liveGraphAreaPoints;
            private set => SetField(ref _liveGraphAreaPoints, value);
        }
        public double GraphMaxWatts { get; private set; } = 100.0;
        public double GraphMidWatts { get; private set; } = 50.0;

        private double _graphHeadX;
        public double GraphHeadX
        {
            get => _graphHeadX;
            private set => SetField(ref _graphHeadX, value);
        }

        private double _graphHeadY;
        public double GraphHeadY
        {
            get => _graphHeadY;
            private set => SetField(ref _graphHeadY, value);
        }

        private bool _hasGraphHead;
        public bool HasGraphHead
        {
            get => _hasGraphHead;
            private set => SetField(ref _hasGraphHead, value);
        }

        public TaskbarItemProgressState TaskbarProgressState
        {
            get
            {
                if (CurrentReading.HasAlert) return TaskbarItemProgressState.Error;
                if (CurrentReading.TotalWatts > 0) return TaskbarItemProgressState.Normal;
                return TaskbarItemProgressState.None;
            }
        }

        public double TaskbarProgressValue
        {
            get
            {
                double max = Settings.OverpowerThresholdWatts > 0 ? Settings.OverpowerThresholdWatts : 150.0;
                return Math.Clamp(CurrentReading.TotalWatts / max, 0.0, 1.0);
            }
        }

        public string TaskbarDescription
        {
            get
            {
                if (CurrentReading.TotalWatts <= 0) return "EcoPower Monitor - Standby";
                return $"EcoPower: {CurrentReading.TotalWatts:F1} W | {CurrentReading.CostPerHour:F2} ฿/hr";
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetField(ref _selectedTabIndex, value) && value == 2)
                {
                    _ = LoadDailyRecordsAsync();
                }
            }
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetField(ref _searchFilter, value))
                {
                    ApplySensorFilter();
                }
            }
        }

        public string? StatusNotification
        {
            get => _statusNotification;
            set => SetField(ref _statusNotification, value);
        }

        public bool IsNotificationVisible
        {
            get => _isNotificationVisible;
            set => SetField(ref _isNotificationVisible, value);
        }

        // Localized String Bindings
        public string T(string key) => _loc.Get(key);

        public string LAppTitle => _loc.Get("AppTitle");
        public string LTabOverview => _loc.Get("TabOverview");
        public string LTabSensors => _loc.Get("TabSensors");
        public string LTabHistory => _loc.Get("TabHistory");
        public string LTabSettings => _loc.Get("TabSettings");
        public string LCurrentPower => _loc.Get("CurrentPower");
        public string LRealtimeCost => _loc.Get("RealtimeCost");
        public string LPerHour => _loc.Get("PerHour");
        public string LPerDay => _loc.Get("PerDay");
        public string LPerMonth => _loc.Get("PerMonth");
        public string LAverageWatts => _loc.Get("AverageWatts");
        public string LPeakWatts => _loc.Get("PeakWatts");
        public string LMinWatts => _loc.Get("MinWatts");
        public string LSessionEnergy => _loc.Get("SessionEnergy");
        public string LSessionCost => _loc.Get("SessionCost");
        public string LActiveTime => _loc.Get("ActiveTime");
        public string LCpuPower => _loc.Get("CpuPower");
        public string LGpuPower => _loc.Get("GpuPower");
        public string LBatteryStatus => _loc.Get("BatteryStatus");
        public string LBatteryRate => _loc.Get("BatteryRate");
        public string LBatteryRemaining => _loc.Get("BatteryRemaining");
        public string LWearLevel => _loc.Get("WearLevel");
        public string LTemperature => _loc.Get("Temperature");
        public string LLoad => _loc.Get("Load");
        public string LDesktopPowerNotice => _loc.Get("DesktopPowerNotice");
        public string LLiveGraphTitle => _loc.Get("LiveGraphTitle");
        public string LSearchPlaceholder => _loc.Get("SearchPlaceholder");
        public string LColHardware => _loc.Get("ColHardware");
        public string LColSensor => _loc.Get("ColSensor");
        public string LColType => _loc.Get("ColType");
        public string LColValue => _loc.Get("ColValue");
        public string LColMin => _loc.Get("ColMin");
        public string LColMax => _loc.Get("ColMax");
        public string LHistoricalSummary => _loc.Get("HistoricalSummary");
        public string LExportCsv => _loc.Get("ExportCsv");
        public string LExportJson => _loc.Get("ExportJson");
        public string LResetSession => _loc.Get("ResetSession");
        public string LColDate => _loc.Get("ColDate");
        public string LColTotalKwh => _loc.Get("ColTotalKwh");
        public string LColTotalCost => $"{_loc.Get("ColTotalCost")} ({Settings.CurrencyCode})";
        public string LColActiveTime => _loc.Get("ColActiveTime");

        // Sensor Category Localization
        public string LCatAll => _loc.Get("CatAll");
        public string LCatPower => _loc.Get("CatPower");
        public string LCatTemp => _loc.Get("CatTemp");
        public string LCatLoad => _loc.Get("CatLoad");
        public string LCatClock => _loc.Get("CatClock");
        public string LCatFan => _loc.Get("CatFan");

        // History KPI Summary Localization
        public string LKpiLifetimeEnergy => _loc.Get("KpiLifetimeEnergy");
        public string LKpiLifetimeCost => _loc.Get("KpiLifetimeCost");
        public string LKpiDailyAvg => _loc.Get("KpiDailyAvg");
        public string LKpiAllTimePeak => _loc.Get("KpiAllTimePeak");

        // Settings & Specs Localization
        public string LGeneralSettings => _loc.Get("GeneralSettings");
        public string LLanguage => _loc.Get("Language");
        public string LTheme => _loc.Get("Theme");
        public string LCurrencyRegion => _loc.Get("CurrencyRegion");
        public string LCurrencyRegionDesc => _loc.Get("CurrencyRegionDesc");
        public string LElectricityTariff => $"{_loc.Get("ElectricityTariff")} ({Settings.CurrencyCode} / kWh)";
        public string LTariffDesc => _loc.Get("TariffDesc");
        public string LSensorRefreshRate => _loc.Get("SensorRefreshRate");
        public string LRefreshRateDesc => _loc.Get("RefreshRateDesc");
        public string LAlertSettings => _loc.Get("AlertSettings");
        public string LOverpowerLimit => _loc.Get("OverpowerLimit");
        public string LOverpowerDesc => _loc.Get("OverpowerDesc");
        public string LHighTempLimit => _loc.Get("HighTempLimit");
        public string LStartWithWindows => _loc.Get("StartWithWindows");
        public string LStartWithWindowsDesc => _loc.Get("StartWithWindowsDesc");
        public string LMinimizeToTray => _loc.Get("MinimizeToTray");
        public string LMinimizeToTrayDesc => _loc.Get("MinimizeToTrayDesc");
        public string LSystemHardwareSpecs => _loc.Get("SystemHardwareSpecs");
        public string LAppInfo => _loc.Get("AppInfo");
        public string LAppDevCredits => _loc.Get("AppDevCredits");
        public string LRestoreDefaults => _loc.Get("RestoreDefaults");
        public string LTrayOpenMain => _loc.Get("TrayOpenMain");
        public string LTrayOpenMini => _loc.Get("TrayOpenMini");
        public string LTrayExit => _loc.Get("TrayExit");

        // Mini HUD Localization & Formatted Live Cost
        public string LMiniPin => _loc.Get("MiniPin");
        public string LMiniUnpin => _loc.Get("MiniUnpin");
        public string LMiniExpand => _loc.Get("MiniExpand");
        public string LMiniClose => _loc.Get("MiniClose");
        public string LMiniPerHr => _loc.Get("MiniPerHr");
        public string FormattedMiniCostText => $"~{CurrentReading.CostPerHour:F2} {Settings.CurrencySymbol}{(_loc.CurrentLanguage == "th" ? "/ชม." : "/hr")}";

        // TOU & Carbon Footprint Telemetry Properties
        public bool IsTouActive => CurrentReading.IsTouActive;
        public bool IsTouOnPeak => CurrentReading.IsTouOnPeak;
        public string TouStatusBadgeText => CurrentReading.IsTouActive 
            ? (CurrentReading.IsTouOnPeak ? (_loc.CurrentLanguage == "th" ? "⚡ ON-PEAK (เรทสูง)" : "⚡ ON-PEAK (Peak)") : (_loc.CurrentLanguage == "th" ? "🍃 OFF-PEAK (ประหยัด)" : "🍃 OFF-PEAK (Eco)")) 
            : (_loc.CurrentLanguage == "th" ? "⚡ STANDARD" : "⚡ STANDARD");
        public string FormattedActiveTariff => $"{CurrentReading.ActiveTariffRate:F2} {Settings.CurrencySymbol}/kWh";
        public string FormattedCarbonPerHour => $"{CurrentReading.CarbonPerHourKg:F3} kg/hr";
        public string FormattedAccumulatedCarbon => $"{CurrentReading.AccumulatedCarbonKg:F3} kg";
        public string FormattedEquivalentTrees => $"{Math.Max(0.001, CurrentReading.AccumulatedCarbonKg / 21.77):F3} {_loc.Get("CarbonTreesUnit")}";

        // TOU & Carbon Localization
        public string LTouTariff => _loc.Get("TouTariff");
        public string LTouTariffDesc => _loc.Get("TouTariffDesc");
        public string LTouOnPeak => _loc.Get("TouOnPeak");
        public string LTouOffPeak => _loc.Get("TouOffPeak");
        public string LTouOnPeakRate => $"{_loc.Get("TouOnPeakRate")} ({Settings.CurrencyCode})";
        public string LTouOffPeakRate => $"{_loc.Get("TouOffPeakRate")} ({Settings.CurrencyCode})";
        public string LCarbonFootprint => _loc.Get("CarbonFootprint");
        public string LCarbonPerHour => _loc.Get("CarbonPerHour");
        public string LCarbonSession => _loc.Get("CarbonSession");
        public string LCarbonTrees => _loc.Get("CarbonTrees");
        public string LCarbonFactor => _loc.Get("CarbonFactor");
        public string LCarbonFactorDesc => _loc.Get("CarbonFactorDesc");
        public string LSaveSettings => _loc.Get("SaveSettings");

        // Top 5 Power Apps Localization
        public string LTopPowerApps => _loc.Get("TopPowerApps");
        public string LTopAppsSub => _loc.Get("TopAppsSub");
        public string LColRank => _loc.Get("ColRank");
        public string LColProgram => _loc.Get("ColProgram");
        public string LColCpuLoad => _loc.Get("ColCpuLoad");
        public string LColEstPower => _loc.Get("ColEstPower");
        public string LColCostHour => _loc.Get("ColCostHour");
        public string LColMemory => _loc.Get("ColMemory");

        // Sensor Category Filtering
        private string _selectedSensorCategory = "All";
        public string SelectedSensorCategory
        {
            get => _selectedSensorCategory;
            set
            {
                if (_selectedSensorCategory != value)
                {
                    _selectedSensorCategory = value;
                    OnPropertyChanged(nameof(SelectedSensorCategory));
                    ApplySensorFilter();
                    OnPropertyChanged(nameof(IsCatAllSelected));
                    OnPropertyChanged(nameof(IsCatPowerSelected));
                    OnPropertyChanged(nameof(IsCatTempSelected));
                    OnPropertyChanged(nameof(IsCatLoadSelected));
                    OnPropertyChanged(nameof(IsCatClockSelected));
                    OnPropertyChanged(nameof(IsCatFanSelected));
                }
            }
        }

        public bool IsCatAllSelected => _selectedSensorCategory == "All";
        public bool IsCatPowerSelected => _selectedSensorCategory == "Power";
        public bool IsCatTempSelected => _selectedSensorCategory == "Temperature";
        public bool IsCatLoadSelected => _selectedSensorCategory == "Load";
        public bool IsCatClockSelected => _selectedSensorCategory == "Clock";
        public bool IsCatFanSelected => _selectedSensorCategory == "Fan";

        public ICommand SelectSensorCategoryCommand => new RelayCommand(param =>
        {
            if (param is string cat)
            {
                SelectedSensorCategory = cat;
            }
        });

        // History KPI Aggregates
        public double LifetimeEnergyKWh => DailyRecords.Sum(r => r.TotalKWh);
        public string FormattedLifetimeEnergy => $"{LifetimeEnergyKWh:F3} kWh";

        public double LifetimeCost => DailyRecords.Sum(r => r.TotalCost);
        public string FormattedLifetimeCost => $"{Settings.CurrencySymbol}{LifetimeCost:F2}";

        public double DailyAverageCost => DailyRecords.Count > 0 ? LifetimeCost / DailyRecords.Count : 0.0;
        public string FormattedDailyAverageCost => $"{Settings.CurrencySymbol}{DailyAverageCost:F2}";

        public double AllTimePeakWatts => DailyRecords.Count > 0 ? DailyRecords.Max(r => r.PeakWatts) : 0.0;
        public string FormattedAllTimePeak => $"{AllTimePeakWatts:F1} W";

        public void NotifyHistoryKpis()
        {
            OnPropertyChanged(nameof(LifetimeEnergyKWh));
            OnPropertyChanged(nameof(FormattedLifetimeEnergy));
            OnPropertyChanged(nameof(LifetimeCost));
            OnPropertyChanged(nameof(FormattedLifetimeCost));
            OnPropertyChanged(nameof(DailyAverageCost));
            OnPropertyChanged(nameof(FormattedDailyAverageCost));
            OnPropertyChanged(nameof(AllTimePeakWatts));
            OnPropertyChanged(nameof(FormattedAllTimePeak));
        }

        public string LanguageDisplayText => Settings.Language.ToUpperInvariant();
        public string LanguageSwitchTargetText => Settings.Language == "th" ? "เปลี่ยนเป็น English" : "Switch to ภาษาไทย";
        public string LanguageTooltip => _loc.CurrentLanguage == "th" ? "สลับภาษา (Switch to English)" : "Switch Language (สลับเป็นภาษาไทย)";
        public string ThemeDisplayText => Settings.Theme;
        public string ThemeSwitchTargetText => Settings.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase)
            ? (_loc.CurrentLanguage == "th" ? "เปลี่ยนเป็น Light Mode" : "Switch to Light Mode")
            : (_loc.CurrentLanguage == "th" ? "เปลี่ยนเป็น Dark Mode" : "Switch to Dark Mode");
        public string ThemeTooltip => Settings.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase)
            ? (_loc.CurrentLanguage == "th" ? "คลิกเพื่อสลับเป็นโหมดสว่าง (Light Mode)" : "Switch to Light Mode")
            : (_loc.CurrentLanguage == "th" ? "คลิกเพื่อสลับเป็นโหมดมืด (Dark Mode)" : "Switch to Dark Mode");
        public bool IsDarkMode => Settings.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase);

        private bool _isWindowMaximized = false;
        public bool IsWindowMaximized
        {
            get => _isWindowMaximized;
            set
            {
                if (_isWindowMaximized != value)
                {
                    _isWindowMaximized = value;
                    OnPropertyChanged(nameof(IsWindowMaximized));
                    OnPropertyChanged(nameof(TooltipMaximizeTitle));
                    OnPropertyChanged(nameof(TooltipMaximizeKey));
                }
            }
        }

        // Universal Caption Tooltip Properties (Dynamic TH/EN with Keyboard Badges)
        public string TooltipMinimizeTitle => _loc.Get("CaptionMinimize");
        public string TooltipMaximizeTitle => IsWindowMaximized ? _loc.Get("CaptionRestore") : _loc.Get("CaptionMaximize");
        public string TooltipMaximizeKey => IsWindowMaximized ? "↓" : "↑";
        public string TooltipCloseTitle => _loc.Get("CaptionClose");

        // Realtime Formatted Cost Telemetry using dynamic active Currency
        public string FormattedCostPerHour => $"{CurrentReading.CostPerHour:F2} {Settings.CurrencySymbol}";
        public string FormattedCostPerDay => $"{CurrentReading.CostPerDay:F2} {Settings.CurrencySymbol}";
        public string FormattedCostPerMonth => $"{CurrentReading.CostPerMonth:F0} {Settings.CurrencySymbol}";
        public string FormattedAccumulatedCost => $"{Statistics.AccumulatedCost:F2} {Settings.CurrencySymbol}";

        // Global Currencies Catalog covering every continent and major economy
        public List<CurrencyOption> AvailableCurrencies { get; } = new()
        {
            new CurrencyOption { Code = "THB", Symbol = "฿", CountryName = "Thailand (บาทไทย)", DefaultTariff = 4.50 },
            new CurrencyOption { Code = "USD", Symbol = "$", CountryName = "United States (US Dollar)", DefaultTariff = 0.16 },
            new CurrencyOption { Code = "EUR", Symbol = "€", CountryName = "European Union (Euro)", DefaultTariff = 0.32 },
            new CurrencyOption { Code = "GBP", Symbol = "£", CountryName = "United Kingdom (British Pound)", DefaultTariff = 0.28 },
            new CurrencyOption { Code = "JPY", Symbol = "¥", CountryName = "Japan (Japanese Yen)", DefaultTariff = 31.0 },
            new CurrencyOption { Code = "CNY", Symbol = "¥", CountryName = "China (Chinese Yuan)", DefaultTariff = 0.60 },
            new CurrencyOption { Code = "KRW", Symbol = "₩", CountryName = "South Korea (Korean Won)", DefaultTariff = 145.0 },
            new CurrencyOption { Code = "TWD", Symbol = "NT$", CountryName = "Taiwan (New Taiwan Dollar)", DefaultTariff = 3.10 },
            new CurrencyOption { Code = "SGD", Symbol = "S$", CountryName = "Singapore (Singapore Dollar)", DefaultTariff = 0.30 },
            new CurrencyOption { Code = "HKD", Symbol = "HK$", CountryName = "Hong Kong (HK Dollar)", DefaultTariff = 1.25 },
            new CurrencyOption { Code = "AUD", Symbol = "A$", CountryName = "Australia (Australian Dollar)", DefaultTariff = 0.35 },
            new CurrencyOption { Code = "CAD", Symbol = "C$", CountryName = "Canada (Canadian Dollar)", DefaultTariff = 0.18 },
            new CurrencyOption { Code = "CHF", Symbol = "CHF", CountryName = "Switzerland (Swiss Franc)", DefaultTariff = 0.27 },
            new CurrencyOption { Code = "INR", Symbol = "₹", CountryName = "India (Indian Rupee)", DefaultTariff = 7.50 },
            new CurrencyOption { Code = "BRL", Symbol = "R$", CountryName = "Brazil (Brazilian Real)", DefaultTariff = 0.85 },
            new CurrencyOption { Code = "MYR", Symbol = "RM", CountryName = "Malaysia (Malaysian Ringgit)", DefaultTariff = 0.45 },
            new CurrencyOption { Code = "IDR", Symbol = "Rp", CountryName = "Indonesia (Indonesian Rupiah)", DefaultTariff = 1500.0 },
            new CurrencyOption { Code = "PHP", Symbol = "₱", CountryName = "Philippines (Philippine Peso)", DefaultTariff = 11.50 },
            new CurrencyOption { Code = "VND", Symbol = "₫", CountryName = "Vietnam (Vietnamese Dong)", DefaultTariff = 2100.0 },
            new CurrencyOption { Code = "NZD", Symbol = "NZ$", CountryName = "New Zealand (NZ Dollar)", DefaultTariff = 0.32 },
            new CurrencyOption { Code = "SEK", Symbol = "kr", CountryName = "Sweden (Swedish Krona)", DefaultTariff = 1.80 },
            new CurrencyOption { Code = "NOK", Symbol = "kr", CountryName = "Norway (Norwegian Krone)", DefaultTariff = 1.60 },
            new CurrencyOption { Code = "DKK", Symbol = "kr", CountryName = "Denmark (Danish Krone)", DefaultTariff = 2.10 },
            new CurrencyOption { Code = "PLN", Symbol = "zł", CountryName = "Poland (Polish Zloty)", DefaultTariff = 0.95 },
            new CurrencyOption { Code = "TRY", Symbol = "₺", CountryName = "Turkey (Turkish Lira)", DefaultTariff = 2.80 },
            new CurrencyOption { Code = "AED", Symbol = "AED", CountryName = "United Arab Emirates (Dirham)", DefaultTariff = 0.30 },
            new CurrencyOption { Code = "SAR", Symbol = "SAR", CountryName = "Saudi Arabia (Riyal)", DefaultTariff = 0.20 },
            new CurrencyOption { Code = "ZAR", Symbol = "R", CountryName = "South Africa (Rand)", DefaultTariff = 2.50 },
            new CurrencyOption { Code = "MXN", Symbol = "$", CountryName = "Mexico (Mexican Peso)", DefaultTariff = 2.10 },
            new CurrencyOption { Code = "ARS", Symbol = "$", CountryName = "Argentina (Argentine Peso)", DefaultTariff = 85.0 }
        };

        private CurrencyOption? _selectedCurrency;
        public CurrencyOption? SelectedCurrency
        {
            get => _selectedCurrency;
            set
            {
                if (SetField(ref _selectedCurrency, value) && value != null)
                {
                    Settings.CurrencyCode = value.Code;
                    Settings.CurrencySymbol = value.Symbol;
                    RefreshCostFormatting();
                }
            }
        }

        public void ApplyCurrencyPreset(CurrencyOption option)
        {
            SelectedCurrency = option;
            Settings.ElectricityRatePerKWh = option.DefaultTariff;
            RefreshCostFormatting();
            ShowNotification(_loc.CurrentLanguage == "th" 
                ? $"เลือกสกุลเงิน {option.Code} ({option.Symbol}) และปรับอัตราค่าไฟเป็น {option.DefaultTariff} เรียบร้อย" 
                : $"Currency set to {option.Code} ({option.Symbol}) with tariff {option.DefaultTariff}");
        }

        public void RefreshCostFormatting()
        {
            OnPropertyChanged(nameof(FormattedCostPerHour));
            OnPropertyChanged(nameof(FormattedCostPerDay));
            OnPropertyChanged(nameof(FormattedCostPerMonth));
            OnPropertyChanged(nameof(FormattedAccumulatedCost));
            OnPropertyChanged(nameof(TaskbarDescription));
            foreach (var r in DailyRecords)
            {
                r.CurrencySymbol = Settings.CurrencySymbol;
            }
        }

        public bool IsAdministrator => new System.Security.Principal.WindowsPrincipal(
            System.Security.Principal.WindowsIdentity.GetCurrent())
            .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

        public bool ShowAdminBanner => !IsAdministrator;

        // Commands
        public ICommand ResetSessionCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand ExportJsonCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand RestoreDefaultSettingsCommand { get; }
        public ICommand ToggleLanguageCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand ToggleStartWithWindowsCommand { get; }
        public ICommand ToggleMinimizeToTrayCommand { get; }
        public ICommand ToggleTouCommand { get; }
        public ICommand RestartAsAdminCommand { get; }

        // Windows 11 Chrome Titlebar Commands
        public ICommand MinimizeWindowCommand { get; }
        public ICommand MaximizeRestoreWindowCommand { get; }
        public ICommand CloseWindowCommand { get; }

        public event Action<string>? ThemeChangeRequested;
        public event Action? MinimizeRequested;

        public MainViewModel(
            IHardwareMonitorService monitorService,
            CostCalculatorService costCalculator,
            DatabaseService databaseService,
            LocalizationService localizationService,
            IProcessMonitorService? processMonitor = null)
        {
            _monitorService = monitorService;
            _costCalculator = costCalculator;
            _databaseService = databaseService;
            _loc = localizationService;
            _processMonitorService = processMonitor ?? new ProcessMonitorService();
            _dispatcher = System.Windows.Application.Current.Dispatcher;

            _loc.LanguageChanged += NotifyAllLanguageProperties;

            ResetSessionCommand = new RelayCommand(ExecuteResetSession);
            ExportCsvCommand = new RelayCommand(async () => await ExecuteExportCsvAsync());
            ExportJsonCommand = new RelayCommand(async () => await ExecuteExportJsonAsync());
            SaveSettingsCommand = new RelayCommand(async () => await ExecuteSaveSettingsAsync());
            RestoreDefaultSettingsCommand = new RelayCommand(ExecuteRestoreDefaults);
            ToggleLanguageCommand = new RelayCommand(ExecuteToggleLanguage);
            ToggleThemeCommand = new RelayCommand(ExecuteToggleTheme);
            ToggleStartWithWindowsCommand = new RelayCommand(ExecuteToggleStartWithWindows);
            ToggleMinimizeToTrayCommand = new RelayCommand(ExecuteToggleMinimizeToTray);
            ToggleTouCommand = new RelayCommand(ExecuteToggleTou);
            RestartAsAdminCommand = new RelayCommand(ExecuteRestartAsAdmin);

            MinimizeWindowCommand = new RelayCommand(() =>
            {
                if (MinimizeRequested != null)
                {
                    MinimizeRequested.Invoke();
                }
                else if (System.Windows.Application.Current.MainWindow is Window mw)
                {
                    mw.WindowState = WindowState.Minimized;
                }
            });

            MaximizeRestoreWindowCommand = new RelayCommand(() =>
            {
                if (System.Windows.Application.Current.MainWindow is Window mw)
                {
                    mw.WindowState = mw.WindowState == WindowState.Maximized 
                        ? WindowState.Normal 
                        : WindowState.Maximized;
                }
            });

            CloseWindowCommand = new RelayCommand(() =>
            {
                if (System.Windows.Application.Current.MainWindow is Window mw)
                {
                    mw.Close();
                }
            });

            // Smooth 30fps Number Interpolator
            _smoothCounterTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _smoothCounterTimer.Tick += (s, e) =>
            {
                double target = _currentReading.TotalWatts;
                double diff = target - _displayedTotalWatts;
                if (Math.Abs(diff) < 0.04)
                {
                    if (_displayedTotalWatts != target)
                    {
                        _displayedTotalWatts = target;
                        OnPropertyChanged(nameof(DisplayedTotalWatts));
                        OnPropertyChanged(nameof(FormattedDisplayedWatts));
                    }
                }
                else
                {
                    _displayedTotalWatts += diff * 0.28;
                    OnPropertyChanged(nameof(DisplayedTotalWatts));
                    OnPropertyChanged(nameof(FormattedDisplayedWatts));
                }
            };
            _smoothCounterTimer.Start();

            // Background Process Telemetry Listener
            _processMonitorService.TopProcessesUpdated += OnTopProcessesUpdated;
            _processMonitorService.Start();

            _monitorService.ReadingUpdated += OnReadingUpdated;

            // Initialize 60 history points with zeros
            for (int i = 0; i < 60; i++)
            {
                _livePowerQueue.Enqueue(0);
            }
            RenderLiveGraph();

            _ = InitializeAsync();
        }

        private void OnTopProcessesUpdated(List<ProcessPowerItem> items)
        {
            _dispatcher.InvokeAsync(() =>
            {
                TopPowerProcesses.Clear();
                foreach (var item in items)
                {
                    TopPowerProcesses.Add(item);
                }
            });
        }

        private async Task InitializeAsync()
        {
            var loaded = await _databaseService.LoadSettingsAsync();
            loaded.StartWithWindows = Services.StartupService.IsRunOnStartupEnabled();
            Settings = loaded;
            _loc.CurrentLanguage = Settings.Language;

            SelectedCurrency = AvailableCurrencies.Find(c => c.Code.Equals(Settings.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                ?? AvailableCurrencies.Find(c => c.Symbol == Settings.CurrencySymbol)
                ?? AvailableCurrencies[0];

            _monitorService.Start(Settings.PollingRateMs);
            await LoadDailyRecordsAsync();
        }

        private void OnReadingUpdated(PowerReading reading, List<SensorItem> sensors)
        {
            _dispatcher.InvokeAsync(() =>
            {
                _costCalculator.ProcessReading(reading, Settings);
                CurrentReading = reading;
                _processMonitorService.UpdateSystemPower(
                    reading.TotalWatts,
                    reading.CpuPackageWatts,
                    reading.CostPerHour,
                    Settings.CurrencySymbol);

                OnPropertyChanged(nameof(TaskbarProgressState));
                OnPropertyChanged(nameof(TaskbarProgressValue));
                OnPropertyChanged(nameof(TaskbarDescription));
                OnPropertyChanged(nameof(FormattedCostPerHour));
                OnPropertyChanged(nameof(FormattedMiniCostText));
                OnPropertyChanged(nameof(FormattedCostPerDay));
                OnPropertyChanged(nameof(FormattedCostPerMonth));
                OnPropertyChanged(nameof(FormattedAccumulatedCost));
                OnPropertyChanged(nameof(IsTouActive));
                OnPropertyChanged(nameof(IsTouOnPeak));
                OnPropertyChanged(nameof(TouStatusBadgeText));
                OnPropertyChanged(nameof(FormattedActiveTariff));
                OnPropertyChanged(nameof(FormattedCarbonPerHour));
                OnPropertyChanged(nameof(FormattedAccumulatedCarbon));
                OnPropertyChanged(nameof(FormattedEquivalentTrees));

                // Save to SQLite database every 60 seconds with accurate Riemann-integral energy delta
                if ((DateTime.Now - _lastDbSave).TotalSeconds >= 60)
                {
                    int elapsedSec = (int)Math.Max(1, Math.Round((DateTime.Now - _lastDbSave).TotalSeconds));
                    _lastDbSave = DateTime.Now;

                    double currentKWh = _costCalculator.Statistics.AccumulatedKWh;
                    double currentCost = _costCalculator.Statistics.AccumulatedCost;
                    double deltaKWh = Math.Max(0.0, currentKWh - _lastSavedKWh);
                    double deltaCost = Math.Max(0.0, currentCost - _lastSavedCost);

                    _lastSavedKWh = currentKWh;
                    _lastSavedCost = currentCost;

                    _ = _databaseService.SaveDailySummaryAsync(
                        DateTime.Today,
                        deltaKWh,
                        deltaCost,
                        _costCalculator.Statistics.PeakWatts,
                        reading.TotalWatts,
                        elapsedSec);
                }

                // Always record telemetry sample into memory buffer regardless of window visibility
                double safeWatts = double.IsFinite(reading.TotalWatts) ? Math.Max(0.0, reading.TotalWatts) : 0.0;
                if (_livePowerQueue.Count >= 60)
                {
                    _livePowerQueue.Dequeue();
                }
                _livePowerQueue.Enqueue(safeWatts);

                // If neither main window nor mini HUD is active (e.g. running silently in background tray), skip UI updates
                if (!_isWindowVisible && !_isMiniHudVisible)
                {
                    return;
                }

                // Skip heavy live graph and sensor collection layout calculations when only Mini HUD is visible
                if (_isWindowVisible)
                {
                    RenderLiveGraph();

                    // Update detailed sensors list in-place (prevents scroll jumps and selection reset)
                    if (DetailedSensors.Count != sensors.Count)
                    {
                        DetailedSensors.Clear();
                        foreach (var s in sensors)
                        {
                            DetailedSensors.Add(s);
                        }
                        ApplySensorFilter();
                    }
                    else
                    {
                        for (int i = 0; i < sensors.Count; i++)
                        {
                            DetailedSensors[i].UpdateValues(sensors[i].Value, sensors[i].MinValue, sensors[i].MaxValue);
                        }
                    }
                }

                OnPropertyChanged(nameof(Statistics));
            });
        }

        private double _currentGraphScaleMax = 60.0;
        private int _scaleHoldCounter = 0;

        private static double QuantizeGraphCeiling(double rawPeak)
        {
            double required = Math.Max(30.0, rawPeak * 1.15);
            if (required <= 40.0) return 40.0;
            if (required <= 60.0) return 60.0;
            if (required <= 80.0) return 80.0;
            if (required <= 100.0) return 100.0;
            if (required <= 150.0) return 150.0;
            if (required <= 200.0) return 200.0;
            if (required <= 250.0) return 250.0;
            if (required <= 300.0) return 300.0;
            if (required <= 400.0) return 400.0;
            if (required <= 500.0) return 500.0;
            if (required <= 750.0) return 750.0;
            return Math.Ceiling(required / 100.0) * 100.0;
        }

        private void RenderLiveGraph()
        {
            double[] values = _livePowerQueue.ToArray();
            double rawPeak = values.Length > 0 ? values.Max() : 0.0;

            double targetScale = QuantizeGraphCeiling(rawPeak);
            if (targetScale > _currentGraphScaleMax)
            {
                // Spikes scale up immediately so the wave never clips
                _currentGraphScaleMax = targetScale;
                _scaleHoldCounter = 0;
            }
            else if (targetScale < _currentGraphScaleMax)
            {
                // Hold ceiling for 10 ticks (approx 10-15s) before stepping down to eliminate scale flutter
                _scaleHoldCounter++;
                if (_scaleHoldCounter >= 10)
                {
                    _currentGraphScaleMax = targetScale;
                    _scaleHoldCounter = 0;
                }
            }
            else
            {
                _scaleHoldCounter = 0;
            }

            double max = _currentGraphScaleMax;
            GraphMaxWatts = Math.Round(max, 0);
            GraphMidWatts = Math.Round(max / 2.0, 0);
            OnPropertyChanged(nameof(GraphMaxWatts));
            OnPropertyChanged(nameof(GraphMidWatts));

            double width = 460.0;
            double height = 120.0;
            double step = values.Length > 1 ? width / (values.Length - 1) : width;

            var newPoints = new PointCollection(values.Length);
            var newAreaPoints = new PointCollection(values.Length + 2);

            newAreaPoints.Add(new Point(0, height));
            Point lastPt = new Point(0, height);
            for (int i = 0; i < values.Length; i++)
            {
                double val = double.IsFinite(values[i]) ? Math.Max(0.0, values[i]) : 0.0;
                double x = i * step;
                double normalizedY = Math.Min(1.0, val / max);
                // Draw with mathematical alignment: top margin 10px, bottom margin 8px
                double y = (height - 8) - (normalizedY * (height - 18));
                var pt = new Point(x, y);
                newPoints.Add(pt);
                newAreaPoints.Add(pt);
                lastPt = pt;
            }
            newAreaPoints.Add(new Point(width, height));

            if (newPoints.Count > 0)
            {
                GraphHeadX = lastPt.X;
                GraphHeadY = lastPt.Y;
                HasGraphHead = true;
            }
            else
            {
                HasGraphHead = false;
            }

            newPoints.Freeze();
            newAreaPoints.Freeze();

            LiveGraphPoints = newPoints;
            LiveGraphAreaPoints = newAreaPoints;
        }

        private void ApplySensorFilter()
        {
            FilteredSensors.Clear();
            string filter = SearchFilter.Trim().ToLowerInvariant();
            string cat = _selectedSensorCategory;

            foreach (var s in DetailedSensors)
            {
                bool matchesCat = cat switch
                {
                    "Power" => s.SensorType.Equals("Power", StringComparison.OrdinalIgnoreCase),
                    "Temperature" => s.SensorType.Equals("Temperature", StringComparison.OrdinalIgnoreCase),
                    "Load" => s.SensorType.Equals("Load", StringComparison.OrdinalIgnoreCase),
                    "Clock" => s.SensorType.Equals("Clock", StringComparison.OrdinalIgnoreCase),
                    "Fan" => s.SensorType.Contains("Fan", StringComparison.OrdinalIgnoreCase) || s.SensorType.Contains("Control", StringComparison.OrdinalIgnoreCase),
                    _ => true
                };

                if (!matchesCat) continue;

                if (string.IsNullOrEmpty(filter) ||
                    s.HardwareName.ToLowerInvariant().Contains(filter) ||
                    s.SensorName.ToLowerInvariant().Contains(filter) ||
                    s.SensorType.ToLowerInvariant().Contains(filter))
                {
                    FilteredSensors.Add(s);
                }
            }
        }

        private async Task LoadDailyRecordsAsync()
        {
            var list = await _databaseService.GetDailySummariesAsync(30);
            DailyRecords.Clear();
            foreach (var r in list)
            {
                r.CurrencySymbol = Settings.CurrencySymbol;
                DailyRecords.Add(r);
            }
            NotifyHistoryKpis();
        }

        public void FlushPendingDatabaseSave()
        {
            try
            {
                double currentKWh = _costCalculator.Statistics.AccumulatedKWh;
                double currentCost = _costCalculator.Statistics.AccumulatedCost;
                double deltaKWh = Math.Max(0.0, currentKWh - _lastSavedKWh);
                double deltaCost = Math.Max(0.0, currentCost - _lastSavedCost);

                if (deltaKWh > 0.0 || deltaCost > 0.0)
                {
                    int elapsedSec = (int)Math.Max(1, Math.Round((DateTime.Now - _lastDbSave).TotalSeconds));
                    _lastDbSave = DateTime.Now;
                    _lastSavedKWh = currentKWh;
                    _lastSavedCost = currentCost;

                    _ = _databaseService.SaveDailySummaryAsync(
                        DateTime.Today,
                        deltaKWh,
                        deltaCost,
                        _costCalculator.Statistics.PeakWatts,
                        _currentReading.TotalWatts,
                        elapsedSec);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error flushing DB save: {ex.Message}");
            }
        }

        private void ExecuteResetSession()
        {
            FlushPendingDatabaseSave();
            _costCalculator.ResetSession();
            _lastSavedKWh = 0.0;
            _lastSavedCost = 0.0;
            OnPropertyChanged(nameof(Statistics));
            ShowNotification(_loc.CurrentLanguage == "th" ? "รีเซ็ตสถิติรอบปัจจุบันเรียบร้อยแล้ว" : "Session reset successfully");
        }

        private async Task ExecuteExportCsvAsync()
        {
            var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"PowerHistory_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (sfd.ShowDialog() == true)
            {
                bool ok = await _databaseService.ExportToCsvAsync(sfd.FileName);
                ShowNotification(ok ? _loc.Get("ExportSuccess") : _loc.Get("ExportFailed"));
            }
        }

        private async Task ExecuteExportJsonAsync()
        {
            var sfd = new SaveFileDialog
            {
                Filter = "JSON File (*.json)|*.json",
                FileName = $"PowerHistory_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (sfd.ShowDialog() == true)
            {
                bool ok = await _databaseService.ExportToJsonAsync(sfd.FileName);
                ShowNotification(ok ? _loc.Get("ExportSuccess") : _loc.Get("ExportFailed"));
            }
        }

        private async Task ExecuteSaveSettingsAsync()
        {
            _monitorService.UpdatePollingRate(Settings.PollingRateMs);
            Services.StartupService.SetRunOnStartup(Settings.StartWithWindows);
            await _databaseService.SaveSettingsAsync(Settings);
            ShowNotification(_loc.Get("SaveSuccess"));
        }

        private void ExecuteRestoreDefaults()
        {
            Settings.ElectricityRatePerKWh = 4.50;
            Settings.CurrencySymbol = "฿";
            Settings.CurrencyCode = "THB";
            Settings.PollingRateMs = 1000;
            Settings.OverpowerThresholdWatts = 350.0;
            Settings.HighTempThresholdCelsius = 85.0;
            Settings.MinimizeToTray = false;
            Settings.StartWithWindows = false;
            Settings.UseTouTariff = false;
            Settings.TouOnPeakRate = 5.80;
            Settings.TouOffPeakRate = 2.65;
            Settings.CarbonEmissionFactor = 0.4999;
            Services.StartupService.SetRunOnStartup(false);

            SelectedCurrency = AvailableCurrencies[0];
            RefreshCostFormatting();
            _ = _databaseService.SaveSettingsAsync(Settings);
            ShowNotification(_loc.CurrentLanguage == "th" ? "คืนค่าการตั้งค่าเริ่มต้นเรียบร้อยแล้ว" : "Default settings restored");
        }

        private void ExecuteToggleStartWithWindows()
        {
            Settings.StartWithWindows = !Settings.StartWithWindows;
            Services.StartupService.SetRunOnStartup(Settings.StartWithWindows);
            OnPropertyChanged(nameof(Settings));
            _ = _databaseService.SaveSettingsAsync(Settings);
            ShowNotification(Settings.StartWithWindows
                ? (_loc.CurrentLanguage == "th" ? "เปิดเริ่มทำงานพร้อม Windows แล้ว" : "Start with Windows enabled")
                : (_loc.CurrentLanguage == "th" ? "ปิดเริ่มทำงานพร้อม Windows แล้ว" : "Start with Windows disabled"));
        }

        private void ExecuteToggleMinimizeToTray()
        {
            Settings.MinimizeToTray = !Settings.MinimizeToTray;
            OnPropertyChanged(nameof(Settings));
            _ = _databaseService.SaveSettingsAsync(Settings);
            ShowNotification(Settings.MinimizeToTray
                ? (_loc.CurrentLanguage == "th" ? "เปิดย่อลง System Tray แล้ว" : "Minimize to System Tray enabled")
                : (_loc.CurrentLanguage == "th" ? "ปิดย่อลง System Tray แล้ว" : "Minimize to System Tray disabled"));
        }

        private void ExecuteToggleTou()
        {
            Settings.UseTouTariff = !Settings.UseTouTariff;
            OnPropertyChanged(nameof(Settings));
            OnPropertyChanged(nameof(IsTouActive));
            OnPropertyChanged(nameof(TouStatusBadgeText));
            _ = _databaseService.SaveSettingsAsync(Settings);
            ShowNotification(Settings.UseTouTariff
                ? (_loc.CurrentLanguage == "th" ? "เปิดใช้งานโมเดลค่าไฟ TOU (Time-of-Use)" : "TOU Tariff Model Activated")
                : (_loc.CurrentLanguage == "th" ? "ปิดใช้งาน TOU (ใช้อัตราค่าไฟคงที่)" : "TOU Tariff Disabled (Flat Rate)"));
        }

        private void ExecuteToggleLanguage()
        {
            Settings.Language = Settings.Language == "th" ? "en" : "th";
            _loc.CurrentLanguage = Settings.Language;
            OnPropertyChanged(nameof(Settings));
            OnPropertyChanged(nameof(LanguageDisplayText));
            OnPropertyChanged(nameof(LanguageSwitchTargetText));
            OnPropertyChanged(nameof(LanguageTooltip));
            OnPropertyChanged(nameof(ThemeSwitchTargetText));
            OnPropertyChanged(nameof(ThemeTooltip));
            OnPropertyChanged(nameof(TooltipMinimizeTitle));
            OnPropertyChanged(nameof(TooltipMaximizeTitle));
            OnPropertyChanged(nameof(TooltipCloseTitle));
            _ = _databaseService.SaveSettingsAsync(Settings);
            ShowNotification(Settings.Language == "th" ? "เปลี่ยนภาษาเป็น ภาษาไทย เรียบร้อยแล้ว" : "Switched language to English");
        }

        private void ExecuteToggleTheme()
        {
            Settings.Theme = Settings.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? "Light" : "Dark";
            ThemeChangeRequested?.Invoke(Settings.Theme);
            OnPropertyChanged(nameof(Settings));
            OnPropertyChanged(nameof(ThemeDisplayText));
            OnPropertyChanged(nameof(ThemeSwitchTargetText));
            OnPropertyChanged(nameof(ThemeTooltip));
            OnPropertyChanged(nameof(IsDarkMode));
            RenderLiveGraph();
            _ = _databaseService.SaveSettingsAsync(Settings);
            ShowNotification(Settings.Theme == "Dark" 
                ? (_loc.CurrentLanguage == "th" ? "เปลี่ยนเป็นโหมดมืด (Dark Mode) เรียบร้อยแล้ว" : "Switched to Dark Mode")
                : (_loc.CurrentLanguage == "th" ? "เปลี่ยนเป็นโหมดสว่าง (Light Mode) เรียบร้อยแล้ว" : "Switched to Light Mode"));
        }

        private void ExecuteRestartAsAdmin()
        {
            try
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    System.Diagnostics.Process.Start(psi);
                    System.Windows.Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"ไม่สามารถยกระดับสิทธิ์ได้: {ex.Message}");
            }
        }

        public void ShowNotification(string message)
        {
            StatusNotification = message;
            IsNotificationVisible = true;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (s, e) =>
            {
                IsNotificationVisible = false;
                timer.Stop();
            };
            timer.Start();
        }

        private void NotifyAllLanguageProperties()
        {
            OnPropertyChanged(nameof(LAppTitle));
            OnPropertyChanged(nameof(LTabOverview));
            OnPropertyChanged(nameof(LTabSensors));
            OnPropertyChanged(nameof(LTabHistory));
            OnPropertyChanged(nameof(LTabSettings));
            OnPropertyChanged(nameof(LCurrentPower));
            OnPropertyChanged(nameof(LRealtimeCost));
            OnPropertyChanged(nameof(LPerHour));
            OnPropertyChanged(nameof(LPerDay));
            OnPropertyChanged(nameof(LPerMonth));
            OnPropertyChanged(nameof(LAverageWatts));
            OnPropertyChanged(nameof(LPeakWatts));
            OnPropertyChanged(nameof(LMinWatts));
            OnPropertyChanged(nameof(LSessionEnergy));
            OnPropertyChanged(nameof(LSessionCost));
            OnPropertyChanged(nameof(LActiveTime));
            OnPropertyChanged(nameof(LCpuPower));
            OnPropertyChanged(nameof(LGpuPower));
            OnPropertyChanged(nameof(LBatteryStatus));
            OnPropertyChanged(nameof(LBatteryRate));
            OnPropertyChanged(nameof(LBatteryRemaining));
            OnPropertyChanged(nameof(LWearLevel));
            OnPropertyChanged(nameof(LTemperature));
            OnPropertyChanged(nameof(LLoad));
            OnPropertyChanged(nameof(LDesktopPowerNotice));
            OnPropertyChanged(nameof(LLiveGraphTitle));
            OnPropertyChanged(nameof(LSearchPlaceholder));
            OnPropertyChanged(nameof(LColHardware));
            OnPropertyChanged(nameof(LColSensor));
            OnPropertyChanged(nameof(LColType));
            OnPropertyChanged(nameof(LColValue));
            OnPropertyChanged(nameof(LColMin));
            OnPropertyChanged(nameof(LColMax));
            OnPropertyChanged(nameof(LHistoricalSummary));
            OnPropertyChanged(nameof(LExportCsv));
            OnPropertyChanged(nameof(LExportJson));
            OnPropertyChanged(nameof(LResetSession));
            OnPropertyChanged(nameof(LColDate));
            OnPropertyChanged(nameof(LColTotalKwh));
            OnPropertyChanged(nameof(LColTotalCost));
            OnPropertyChanged(nameof(LColActiveTime));
            OnPropertyChanged(nameof(LGeneralSettings));
            OnPropertyChanged(nameof(LLanguage));
            OnPropertyChanged(nameof(LTheme));
            OnPropertyChanged(nameof(LElectricityTariff));
            OnPropertyChanged(nameof(LSensorRefreshRate));
            OnPropertyChanged(nameof(LAlertSettings));
            OnPropertyChanged(nameof(LOverpowerLimit));
            OnPropertyChanged(nameof(LHighTempLimit));
            OnPropertyChanged(nameof(LStartWithWindows));
            OnPropertyChanged(nameof(LStartWithWindowsDesc));
            OnPropertyChanged(nameof(LMinimizeToTray));
            OnPropertyChanged(nameof(LMinimizeToTrayDesc));
            OnPropertyChanged(nameof(LTrayOpenMain));
            OnPropertyChanged(nameof(LTrayOpenMini));
            OnPropertyChanged(nameof(LTrayExit));
            OnPropertyChanged(nameof(LanguageDisplayText));
            OnPropertyChanged(nameof(LanguageSwitchTargetText));
            OnPropertyChanged(nameof(LanguageTooltip));
            OnPropertyChanged(nameof(LCatAll));
            OnPropertyChanged(nameof(LCatPower));
            OnPropertyChanged(nameof(LCatTemp));
            OnPropertyChanged(nameof(LCatLoad));
            OnPropertyChanged(nameof(LCatClock));
            OnPropertyChanged(nameof(LCatFan));
            OnPropertyChanged(nameof(LKpiLifetimeEnergy));
            OnPropertyChanged(nameof(LKpiLifetimeCost));
            OnPropertyChanged(nameof(LKpiDailyAvg));
            OnPropertyChanged(nameof(LKpiAllTimePeak));
            OnPropertyChanged(nameof(LCurrencyRegion));
            OnPropertyChanged(nameof(LCurrencyRegionDesc));
            OnPropertyChanged(nameof(LTariffDesc));
            OnPropertyChanged(nameof(LRefreshRateDesc));
            OnPropertyChanged(nameof(LOverpowerDesc));
            OnPropertyChanged(nameof(LSystemHardwareSpecs));
            OnPropertyChanged(nameof(LAppInfo));
            OnPropertyChanged(nameof(LAppDevCredits));
            OnPropertyChanged(nameof(LRestoreDefaults));
            OnPropertyChanged(nameof(ThemeDisplayText));
            OnPropertyChanged(nameof(ThemeSwitchTargetText));
            OnPropertyChanged(nameof(ThemeTooltip));
            OnPropertyChanged(nameof(IsDarkMode));
            OnPropertyChanged(nameof(TouStatusBadgeText));
            OnPropertyChanged(nameof(LTouTariff));
            OnPropertyChanged(nameof(LTouTariffDesc));
            OnPropertyChanged(nameof(LTouOnPeak));
            OnPropertyChanged(nameof(LTouOffPeak));
            OnPropertyChanged(nameof(LTouOnPeakRate));
            OnPropertyChanged(nameof(LTouOffPeakRate));
            OnPropertyChanged(nameof(LCarbonFootprint));
            OnPropertyChanged(nameof(LCarbonPerHour));
            OnPropertyChanged(nameof(LCarbonSession));
            OnPropertyChanged(nameof(LCarbonTrees));
            OnPropertyChanged(nameof(LCarbonFactor));
            OnPropertyChanged(nameof(LCarbonFactorDesc));
            OnPropertyChanged(nameof(LTopPowerApps));
            OnPropertyChanged(nameof(LTopAppsSub));
            OnPropertyChanged(nameof(LColRank));
            OnPropertyChanged(nameof(LColProgram));
            OnPropertyChanged(nameof(LColCpuLoad));
            OnPropertyChanged(nameof(LColEstPower));
            OnPropertyChanged(nameof(LColCostHour));
            OnPropertyChanged(nameof(LColMemory));
            OnPropertyChanged(nameof(FormattedEquivalentTrees));
            OnPropertyChanged(nameof(LSaveSettings));
            OnPropertyChanged(nameof(LMiniPin));
            OnPropertyChanged(nameof(LMiniUnpin));
            OnPropertyChanged(nameof(LMiniExpand));
            OnPropertyChanged(nameof(LMiniClose));
            OnPropertyChanged(nameof(LMiniPerHr));
            OnPropertyChanged(nameof(FormattedMiniCostText));
        }

        public void Cleanup()
        {
            try
            {
                _smoothCounterTimer?.Stop();
                _processMonitorService?.Stop();
            }
            catch { }
        }
    }
}

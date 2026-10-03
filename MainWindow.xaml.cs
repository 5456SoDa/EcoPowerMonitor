using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using EcoPowerMonitor.Services;
using EcoPowerMonitor.ViewModels;

namespace EcoPowerMonitor
{
    public partial class MainWindow : Window
    {
        private readonly IHardwareMonitorService _hardwareMonitor;
        private readonly CostCalculatorService _costCalculator;
        private readonly DatabaseService _databaseService;
        private readonly LocalizationService _localizationService;
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _hardwareMonitor = HardwareMonitorFactory.Create();
            _costCalculator = new CostCalculatorService();
            _databaseService = new DatabaseService();
            _localizationService = new LocalizationService();

            _viewModel = new MainViewModel(
                _hardwareMonitor,
                _costCalculator,
                _databaseService,
                _localizationService);

            _viewModel.ThemeChangeRequested += theme =>
            {
                App.ApplyTheme(theme);
                WindowThemeService.ApplyTitlebarTheme(this, theme.Equals("Dark", StringComparison.OrdinalIgnoreCase));
            };

            _viewModel.MinimizeRequested += () => WindowState = WindowState.Minimized;

            SourceInitialized += (s, e) =>
            {
                WindowThemeService.ApplyTitlebarTheme(this, _viewModel.Settings.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase));
                WindowThemeService.EnableWindowDwmAnimations(this);
            };

            IsVisibleChanged += (s, e) =>
            {
                _viewModel.IsWindowVisible = IsVisible && WindowState != WindowState.Minimized;
            };

            DataContext = _viewModel;

            InitializeTrayIcon();
        }

        private Views.MiniWidgetWindow? _miniWidget;
        private System.Windows.Forms.NotifyIcon? _notifyIcon;

        private void InitializeTrayIcon()
        {
            try
            {
                _notifyIcon = new System.Windows.Forms.NotifyIcon();

                var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                }
                else
                {
                    var exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
                    {
                        _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                    }
                    else
                    {
                        _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                    }
                }

                _notifyIcon.Text = "EcoPower Monitor";
                _notifyIcon.Visible = true;

                // Double click restores main window
                _notifyIcon.DoubleClick += (s, e) => RestoreMainWindow();

                // Context Menu
                var contextMenu = new System.Windows.Forms.ContextMenuStrip();

                var itemOpen = new System.Windows.Forms.ToolStripMenuItem();
                itemOpen.Click += (s, e) => RestoreMainWindow();

                var itemMini = new System.Windows.Forms.ToolStripMenuItem();
                itemMini.Click += (s, e) => OpenMiniHud();

                var itemExit = new System.Windows.Forms.ToolStripMenuItem();
                itemExit.Click += (s, e) =>
                {
                    try
                    {
                        _viewModel.FlushPendingDatabaseSave();
                        _viewModel.Cleanup();
                        if (_notifyIcon != null)
                        {
                            _notifyIcon.Visible = false;
                            _notifyIcon.Dispose();
                        }
                    }
                    catch { }
                    System.Windows.Application.Current.Shutdown();
                };

                void UpdateTrayMenuTexts()
                {
                    itemOpen.Text = _localizationService.Get("TrayOpenMain");
                    itemMini.Text = _localizationService.Get("TrayOpenMini");
                    itemExit.Text = _localizationService.Get("TrayExit");
                }
                UpdateTrayMenuTexts();
                _localizationService.LanguageChanged += UpdateTrayMenuTexts;

                contextMenu.Items.Add(itemOpen);
                contextMenu.Items.Add(itemMini);
                contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                contextMenu.Items.Add(itemExit);

                _notifyIcon.ContextMenuStrip = contextMenu;

                // Update tooltip on reading changed
                _viewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(_viewModel.CurrentReading) && _notifyIcon != null)
                    {
                        var w = _viewModel.CurrentReading.TotalWatts;
                        var c = _viewModel.CurrentReading.CostPerHour;
                        string tip = $"EcoPower: {w:F1}W | {c:F2}{_viewModel.Settings.CurrencySymbol}/hr";
                        if (tip.Length > 63) tip = tip.Substring(0, 63);
                        _notifyIcon.Text = tip;
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to init tray icon: {ex.Message}");
            }
        }

        private void RestoreMainWindow()
        {
            _miniWidget?.Hide();
            _viewModel.IsMiniHudVisible = false;
            Show();
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            _viewModel.IsWindowVisible = true;
            Activate();
            Focus();
        }

        private void OpenMiniHud()
        {
            _viewModel.IsMiniHudVisible = true;
            _viewModel.IsWindowVisible = false;
            if (_miniWidget == null)
            {
                _miniWidget = new Views.MiniWidgetWindow(this, DataContext);
                _miniWidget.Closed += (s, e) =>
                {
                    _miniWidget = null;
                    _viewModel.IsMiniHudVisible = false;
                };
            }
            _miniWidget.Show();
            Hide();
        }

        private void BtnMiniHud_Click(object sender, RoutedEventArgs e)
        {
            OpenMiniHud();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Minimized)
            {
                // Throttle background UI rendering to 0.0% CPU while minimized
                _viewModel.IsWindowVisible = false;
            }
            else
            {
                _viewModel.IsWindowVisible = IsVisible;
            }

            _viewModel.IsWindowMaximized = WindowState == WindowState.Maximized;

            if (BtnMaximizeIcon != null && MainRootBorder != null)
            {
                if (WindowState == WindowState.Maximized)
                {
                    MainRootBorder.Padding = new Thickness(7);
                    BtnMaximizeIcon.Data = (Geometry)FindResource("IconWindowRestoreGeometry");
                }
                else
                {
                    MainRootBorder.Padding = new Thickness(0);
                    BtnMaximizeIcon.Data = (Geometry)FindResource("IconWindowMaxGeometry");
                }
            }
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == System.Windows.Input.Key.F11)
            {
                _viewModel.MaximizeRestoreWindowCommand.Execute(null);
                e.Handled = true;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // If user enabled Close-to-Tray, minimize to notification area instead of exiting
            if (_viewModel.Settings.MinimizeToTray)
            {
                e.Cancel = true;
                Hide();
                _miniWidget?.Hide();
                _viewModel.IsWindowVisible = false;
                _viewModel.IsMiniHudVisible = false;
                _viewModel.ShowNotification(_viewModel.Settings.Language == "th" 
                    ? "EcoPower ย่อทำงานต่อใน System Tray (ถาดขวาล่าง)" 
                    : "EcoPower is running in the background tray");
                return;
            }

            base.OnClosing(e);
            try
            {
                _viewModel.FlushPendingDatabaseSave();
                _viewModel.Cleanup();
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
                _miniWidget?.Close();
                _hardwareMonitor.Dispose();
            }
            catch { }
        }
    }
}
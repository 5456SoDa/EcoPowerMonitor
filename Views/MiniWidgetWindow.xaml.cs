using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using EcoPowerMonitor.ViewModels;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace EcoPowerMonitor.Views
{
    public partial class MiniWidgetWindow : Window
    {
        private readonly Window _mainWindow;

        public MiniWidgetWindow(Window mainWindow, object dataContext)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            DataContext = dataContext;

            // Position at top-right corner of screen by default with multi-monitor safety
            double screenWidth = SystemParameters.WorkArea.Width;
            Left = Math.Max(20, screenWidth - Width - 30);
            Top = 40;

            UpdatePinVisual();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Double-click anywhere on the background expands back to full dashboard
            if (e.ClickCount == 2)
            {
                BtnExpand_Click(sender, e);
                return;
            }

            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnPin_Click(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
            UpdatePinVisual();
        }

        private void UpdatePinVisual()
        {
            if (DataContext is MainViewModel vm)
            {
                BtnPin.ToolTip = Topmost ? vm.LMiniUnpin : vm.LMiniPin;
            }
            else
            {
                BtnPin.ToolTip = Topmost ? "ยกเลิกการปักหมุด" : "ปักหมุดให้อยู่บนสุด";
            }

            if (Topmost)
            {
                BtnPin.Background = (Brush)FindResource("AccentSubtleBrush");
                BtnPin.BorderBrush = (Brush)FindResource("CyanAccentBrush");
                TxtPin.Opacity = 1.0;
            }
            else
            {
                BtnPin.Background = Brushes.Transparent;
                BtnPin.BorderBrush = (Brush)FindResource("CardBorderBrush");
                TxtPin.Opacity = 0.45;
            }
        }

        private void BtnExpand_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.IsMiniHudVisible = false;
            }
            _mainWindow.Show();
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
            _mainWindow.Activate();
            _mainWindow.Focus();
            Hide();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            // Delegate close to MainWindow to strictly honor:
            // 1) Database energy/cost Riemann-sum flush
            // 2) Minimize-to-Tray user preference
            // 3) Hardware monitor & Tray Icon clean disposal
            _mainWindow.Close();
        }
    }
}

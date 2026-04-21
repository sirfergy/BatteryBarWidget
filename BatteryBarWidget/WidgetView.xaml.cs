using System;
using Microsoft.Gaming.XboxGameBar;
using Windows.Devices.Power;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace BatteryBarWidget
{
    public sealed partial class WidgetView : Page
    {
        private XboxGameBarWidget? _widget;
        private DispatcherTimer? _timer;
        private readonly Battery _battery = Battery.AggregateBattery;

        private static readonly SolidColorBrush DischargeBrush =
            new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x8A, 0x80));
        private static readonly SolidColorBrush ChargeBrush =
            new SolidColorBrush(Color.FromArgb(0xFF, 0x81, 0xC7, 0x84));
        private static readonly SolidColorBrush IdleBrush =
            new SolidColorBrush(Color.FromArgb(0xFF, 0xA0, 0xA0, 0xA0));

        public WidgetView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _widget = e.Parameter as XboxGameBarWidget;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _battery.ReportUpdated += OnBatteryReportUpdated;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _timer.Tick += (_, __) => UpdateBattery();
            _timer.Start();
            UpdateBattery();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _battery.ReportUpdated -= OnBatteryReportUpdated;
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
        }

        private async void OnBatteryReportUpdated(Battery sender, object args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, UpdateBattery);
        }

        private void UpdateBattery()
        {
            try
            {
                var report = _battery.GetReport();
                var rateMw = report.ChargeRateInMilliwatts;
                var remaining = report.RemainingCapacityInMilliwattHours;
                var full = report.FullChargeCapacityInMilliwattHours;

                if (remaining.HasValue && full.HasValue && full.Value > 0)
                {
                    var pct = (int)Math.Round(remaining.Value * 100.0 / full.Value);
                    PercentText.Text = $"{pct}%";
                }
                else
                {
                    PercentText.Text = "—";
                }

                if (!rateMw.HasValue || Math.Abs(rateMw.Value) < 50)
                {
                    RateText.Foreground = IdleBrush;
                    RateText.Text = "idle";
                    return;
                }

                double watts = rateMw.Value / 1000.0;
                if (watts > 0)
                {
                    RateText.Foreground = ChargeBrush;
                    RateText.Text = $"{watts:0.0} W";
                }
                else
                {
                    RateText.Foreground = DischargeBrush;
                    RateText.Text = $"{Math.Abs(watts):0.0} W";
                }
            }
            catch
            {
                PercentText.Text = "—";
                RateText.Text = "—";
            }
        }
    }
}

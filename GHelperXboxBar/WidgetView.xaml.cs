using System;
using System.Threading.Tasks;
using Microsoft.Gaming.XboxGameBar;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace GHelperXboxBar
{
    public sealed partial class WidgetView : Page
    {
        private XboxGameBarWidget? _widget;

        public WidgetView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _widget = e.Parameter as XboxGameBarWidget;
            StatusText.Text = "Ready";
        }

        private async void OnProfileClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is null) return;
            var tag = btn.Tag.ToString() ?? string.Empty;
            await InvokeHotkeyAsync(tag, btn.Content?.ToString() ?? tag);
        }

        private async void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            await InvokeHotkeyAsync("reload", "Reload config");
        }

        private async Task InvokeHotkeyAsync(string action, string label)
        {
            try
            {
                this.IsEnabled = false;
                StatusText.Text = $"{label}…";

                // Pass the requested action to the desktop extension via LocalSettings.
                var settings = ApplicationData.Current.LocalSettings;
                settings.Values["action"] = action;
                settings.Values["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();

                StatusText.Text = $"{label} ✓";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error: " + ex.Message;
            }
            finally
            {
                this.IsEnabled = true;
            }
        }
    }
}

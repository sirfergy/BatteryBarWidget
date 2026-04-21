using Microsoft.Gaming.XboxGameBar;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace BatteryBarWidget
{
    public sealed partial class App : Application
    {
        private XboxGameBarWidget? _widget;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind != ActivationKind.Protocol)
            {
                return;
            }

            var protocolArgs = args as IProtocolActivatedEventArgs;
            if (protocolArgs == null || !protocolArgs.Uri.Scheme.StartsWith("ms-gamebarwidget"))
            {
                return;
            }

            if (args is not XboxGameBarWidgetActivatedEventArgs widgetArgs)
            {
                return;
            }

            if (!widgetArgs.IsLaunchActivation)
            {
                return;
            }

            var rootFrame = new Frame();
            Window.Current.Content = rootFrame;
            _widget = new XboxGameBarWidget(widgetArgs, Window.Current.CoreWindow, rootFrame);
            rootFrame.Navigate(typeof(WidgetView), _widget);
            Window.Current.Activate();
        }
    }
}

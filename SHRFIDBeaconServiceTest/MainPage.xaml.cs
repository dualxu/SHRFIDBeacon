using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SHRFIDBeaconServiceTest
{

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        //when start Navigation, Add Native object to webview,and then the object can be invoke in html.
        private void MainWebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            //This way can invoke native method and get result from native method.
            //But because this native is from orther project in this solution, so when you operator UI, it will be very difficult.
            sender.AddWebAllowedObject("SHRFIDBeaconService", new SHRFIDBeaconRuntimeComponent.SHRFIDBeaconService());
        }

        private void Back_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            //Frame.Navigate(typeof(MainPage));
        }
    }
}

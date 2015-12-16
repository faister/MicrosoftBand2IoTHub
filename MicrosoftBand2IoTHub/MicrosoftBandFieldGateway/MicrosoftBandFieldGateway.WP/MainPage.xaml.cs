using IoTHub;
using Microsoft.Band;
using Microsoft.Band.Sensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MicrosoftBandFieldGateway
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        // Used for refreshing the number of samples received when the app is visible
        private static DispatcherTimer _refreshTimer;


        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.DataContext = this.viewModel = App.Current;

            this.viewModel.ButtonContent = "Start";

            // AppSetting contains DeviceID, IoT Hub Hostname and Device Key config settings
            if (AppSettings == null) AppSettings = new AppSettings();

            this.bandTelemetry = new MicrosoftBandTelemetry();

            //ResetTelemetryReading();

            // Setup a timer to periodically refresh results when the app is visible.
            _refreshTimer = new DispatcherTimer()
            {
                Interval = new TimeSpan(0, 0, 1) // Refresh once every second
            };
            _refreshTimer.Tick += RefreshTimer_Tick;


            _geolocator = new Geolocator();
            // Desired Accuracy needs to be set
            // before polling for desired accuracy.
            _geolocator.DesiredAccuracyInMeters = 10;
            GetGeolocation();

        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.

            _refreshTimer.Start();
            // Store a setting for the background task to read
            ApplicationData.Current.LocalSettings.Values["IsAppVisible"] = true;

        }

        /// <summary>
        /// This is the event handler for VisibilityChanged events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">
        /// Event data that can be examined for the current visibility state.
        /// </param>
        private void VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["IsAppVisible"] = e.Visible;

            if (e.Visible)
            {
                _refreshTimer.Start();
            }
            else
            {
                _refreshTimer.Stop();
            }
        }

        /// <summary>
        /// This is the tick handler for the Refresh timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshTimer_Tick(object sender, object e)
        {
            // Keeps checking until we have GPS coordinates locked, upon which we will update the device info with IoT Hub
            if (latitude == 0 || longitude == 0)
            {
                GetGeolocation();
            }

            if ((latitude > 0 || longitude > 0) && !IsDeviceInfoUpdated && !String.IsNullOrEmpty(FWVersion) && !String.IsNullOrEmpty(HWVersion))
            {
                IoTHubHttpServiceManager.UpdateDeviceInfo(latitude, longitude, FWVersion, HWVersion);
                IsDeviceInfoUpdated = true;
            }

            //HRText.Text = ApplicationData.Current.LocalSettings.Values["HeartRate"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["HeartRate"].ToString();
            //SkinTempText.Text = ApplicationData.Current.LocalSettings.Values["SkinTemperature"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["SkinTemperature"].ToString();
            //PedometerText.Text = ApplicationData.Current.LocalSettings.Values["Pedometer"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["Pedometer"].ToString();
            //DistanceText.Text = ApplicationData.Current.LocalSettings.Values["Distance"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["Distance"].ToString();
            //CaloriesText.Text = ApplicationData.Current.LocalSettings.Values["Calories"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["Calories"].ToString();
            //BarometerText.Text = ApplicationData.Current.LocalSettings.Values["AirPressure"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["AirPressure"].ToString();
            //GSRText.Text = ApplicationData.Current.LocalSettings.Values["GsrResistance"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["GsrResistance"].ToString();
            //AmbientLightingText.Text = ApplicationData.Current.LocalSettings.Values["Brightness"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["Brightness"].ToString();
            //AltimeterRateText.Text = ApplicationData.Current.LocalSettings.Values["AltimeterRate"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["AltimeterRate"].ToString();
        }

        private void ResetTelemetryReading()
        {
            ApplicationData.Current.LocalSettings.Values["HeartRate"] = null;
            ApplicationData.Current.LocalSettings.Values["SkinTemperature"] = null;
            ApplicationData.Current.LocalSettings.Values["Pedometer"] = null;
            ApplicationData.Current.LocalSettings.Values["Distance"] = null;
            ApplicationData.Current.LocalSettings.Values["Calories"] = null;
            ApplicationData.Current.LocalSettings.Values["AirPressure"] = null;
            ApplicationData.Current.LocalSettings.Values["GsrResistance"] = null;
            ApplicationData.Current.LocalSettings.Values["Brightness"] = null;
            ApplicationData.Current.LocalSettings.Values["AltimeterRate"] = null;
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage));
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AppSettingsPage));
        }
    }
}

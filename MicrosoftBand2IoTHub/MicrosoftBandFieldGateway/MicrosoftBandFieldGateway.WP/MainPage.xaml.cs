using IoTHub;
using Microsoft.Band;
using Microsoft.Band.Sensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
using Windows.Devices.Geolocation;
using System.Threading;

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
        private static IoTHubHttpServiceManager IoTHubHttpServiceManager;
        private static AppSettings AppSettings;

        private IBandClient bandClient;

        // for geolocation
        private Geolocator _geolocator = null;
        private CancellationTokenSource _cts = null;
        private double latitude, longitude;
        private bool IsDeviceInfoUpdated = false;

        public string FWVersion;
        public string HWVersion;
        public string ConnectedBandName;


        //private IBandInfo[] pairedBands;
        private MicrosoftBandTelemetry bandTelemetry;
        int samplesReceived = 0; // the number of HeartRate samples received

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            //this.InitializeBandServiceManager();

            ResetTelemetryReading();

            // Setup a timer to periodically refresh results when the app is visible.
            _refreshTimer = new DispatcherTimer()
            {
                Interval = new TimeSpan(0, 0, 1) // Refresh once every second
            };
            _refreshTimer.Tick += RefreshTimer_Tick;

            bandTelemetry = new MicrosoftBandTelemetry();
            //IoTHubServiceManager.Initialize();

            // AppSetting contains DeviceID, IoT Hub Hostname and Device Key config settings
            if (AppSettings == null) AppSettings = new AppSettings();

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

            HRText.Text = ApplicationData.Current.LocalSettings.Values["HeartRate"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["HeartRate"].ToString();
            SkinTempText.Text = ApplicationData.Current.LocalSettings.Values["SkinTemperature"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["SkinTemperature"].ToString();
            PedometerText.Text = ApplicationData.Current.LocalSettings.Values["Pedometer"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["Pedometer"].ToString();
            DistanceText.Text = ApplicationData.Current.LocalSettings.Values["Distance"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["Distance"].ToString();
            CaloriesText.Text = ApplicationData.Current.LocalSettings.Values["Calories"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["Calories"].ToString();
            BarometerText.Text = ApplicationData.Current.LocalSettings.Values["AirPressure"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["AirPressure"].ToString();
            GSRText.Text = ApplicationData.Current.LocalSettings.Values["GsrResistance"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["GsrResistance"].ToString();
            AmbientLightingText.Text = ApplicationData.Current.LocalSettings.Values["Brightness"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["Brightness"].ToString();
            AltimeterRateText.Text = ApplicationData.Current.LocalSettings.Values["AltimeterRate"] == null ? "N/A" : ApplicationData.Current.LocalSettings.Values["AltimeterRate"].ToString();
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

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Change UI captions
            if (StartButton.Content.Equals("Start"))
            {
                StartButton.Content = "Stop";
                MicrosoftBandTextBlock.Text = "Searching for Band....";
            }
            else
            {
                await bandClient.SensorManager.HeartRate.StopReadingsAsync();
                await bandClient.SensorManager.SkinTemperature.StopReadingsAsync();
                await bandClient.SensorManager.Calories.StopReadingsAsync();
                await bandClient.SensorManager.Distance.StopReadingsAsync();

                if (bandClient.SensorManager.Pedometer.IsSupported) await bandClient.SensorManager.Pedometer.StopReadingsAsync();
                if (bandClient.SensorManager.Barometer.IsSupported) await bandClient.SensorManager.Barometer.StopReadingsAsync();
                if (bandClient.SensorManager.Gsr.IsSupported) await bandClient.SensorManager.Gsr.StopReadingsAsync();
                if (bandClient.SensorManager.AmbientLight.IsSupported) await bandClient.SensorManager.AmbientLight.StopReadingsAsync();
                if (bandClient.SensorManager.Altimeter.IsSupported) await bandClient.SensorManager.Altimeter.StopReadingsAsync();

                StartButton.Content = "Start";
                MicrosoftBandTextBlock.Text = string.Format("Done. {0} Microsoft Band telemetry were ingested.", samplesReceived);
                this.ResetTelemetryReading();
                return;
            }

            //string hostname = "sydneyremote.azure-devices.net";
            //string deviceid = "SydneyBand";
            //string key = "u9rF4kfsXktrI9QHvzlmqA==";

            if (IoTHubHttpServiceManager == null)
            {
                //IoTHubHttpServiceManager = new IoTHubHttpServiceManager(hostname, deviceid, key);
                IoTHubHttpServiceManager = new IoTHubHttpServiceManager(AppSettings.IoTHubHostName, AppSettings.DeviceID, AppSettings.DeviceKey);
            }
            try
            {
                IBandInfo[] pairedBands = await BandClientManager.Instance.GetBandsAsync();
                if (pairedBands.Length < 1)
                {
                    MicrosoftBandTextBlock.Text = "This sample app requires a Microsoft Band paired to your device. Also make sure that you have the latest firmware installed on your Band, as provided by the latest Microsoft Health app.";
                    StartButton.Visibility = Visibility.Collapsed;
                    return;
                }

                using (bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]))
                {
                    // do work after successful connect     
                    ConnectedBandName = pairedBands[0].Name;
                    MicrosoftBandTextBlock.Text = "Connected: " + ConnectedBandName;
                    
                    FWVersion = await bandClient.GetFirmwareVersionAsync();
                    HWVersion = await bandClient.GetHardwareVersionAsync();

                    // check current user heart rate consent 
                    if (bandClient.SensorManager.HeartRate.GetCurrentUserConsent() != UserConsent.Granted)
                    {
                        // user hasn’t consented, request consent  
                        await bandClient.SensorManager.HeartRate.RequestUserConsentAsync();
                    }

                    bool heartRateConsentGranted;

                    // Check whether the user has granted access to the HeartRate sensor.
                    if (bandClient.SensorManager.HeartRate.GetCurrentUserConsent() == UserConsent.Granted)
                    {
                        heartRateConsentGranted = true;
                    }
                    else
                    {
                        heartRateConsentGranted = await bandClient.SensorManager.HeartRate.RequestUserConsentAsync();
                    }

                    if (!heartRateConsentGranted)
                    {
                        MicrosoftBandTextBlock.Text = "Access to the heart rate sensor is denied.";
                        return;
                    }

                    // Subscribe to HeartRate data.
                    //bandClient.SensorManager.HeartRate.ReadingChanged += (s, args) => { samplesReceived++; };
                    //await bandClient.SensorManager.HeartRate.StartReadingsAsync();

                    // hook up to the Heartrate sensor ReadingChanged event 
                    bandClient.SensorManager.HeartRate.ReadingChanged += HeartRate_ReadingChanged;
                    await bandClient.SensorManager.HeartRate.StartReadingsAsync();

                    // hook up to the Skin temperature sensor ReadingChanged event 
                    bandClient.SensorManager.SkinTemperature.ReadingChanged += SkinTemperature_ReadingChanged;
                    await bandClient.SensorManager.SkinTemperature.StartReadingsAsync();

                    // hook up to the Distance sensor ReadingChanged event 
                    bandClient.SensorManager.Distance.ReadingChanged += Distance_ReadingChanged;
                    await bandClient.SensorManager.Distance.StartReadingsAsync();

                    // hook up to the Calories sensor ReadingChanged event 
                    bandClient.SensorManager.Calories.ReadingChanged += Calories_ReadingChanged;
                    await bandClient.SensorManager.Calories.StartReadingsAsync();

                    if (bandClient.SensorManager.Pedometer.IsSupported)
                    {
                        // Subscribe to Altimeter data.
                        bandClient.SensorManager.Pedometer.ReadingChanged += Pedometer_ReadingChanged;
                        await bandClient.SensorManager.Pedometer.StartReadingsAsync();
                    }

                    if (bandClient.SensorManager.Barometer.IsSupported)
                    {
                        ApplicationData.Current.LocalSettings.Values["AirPressure"] = "Band2 only";
                        // Subscribe to Barometer data. - (Microsoft Band 2 only) Provides the current raw air pressure in hPa (hectopascals) and raw temperature in degrees Celsius.
                        bandClient.SensorManager.Barometer.ReadingChanged += Barometer_ReadingChanged;
                        await bandClient.SensorManager.Barometer.StartReadingsAsync();
                    }
                    if (bandClient.SensorManager.Gsr.IsSupported)
                    {
                        ApplicationData.Current.LocalSettings.Values["GsrResistance"] = "Band2 only";
                        // Subscribe to Galvanic Skin Response. - (Microsoft Band 2 only) Provides the current skin resistance of the wearer in kohms.
                        bandClient.SensorManager.Gsr.ReadingChanged += Gsr_ReadingChanged;
                        await bandClient.SensorManager.Gsr.StartReadingsAsync();
                    }

                    if (bandClient.SensorManager.AmbientLight.IsSupported)
                    {
                        ApplicationData.Current.LocalSettings.Values["Brightness"] = "Band2 only";
                        // Subscribe to Ambient Light - (Microsoft Band 2 only) Provides the current light intensity (illuminance) in lux (Lumes per sq. meter).
                        bandClient.SensorManager.AmbientLight.ReadingChanged += AmbientLight_ReadingChanged;
                        await bandClient.SensorManager.AmbientLight.StartReadingsAsync();
                    }

                    if (bandClient.SensorManager.Altimeter.IsSupported)
                    {
                        ApplicationData.Current.LocalSettings.Values["AltimeterRate"] = "Band2 only";
                        // Subscribe to Altimeter - (Microsoft Band 2 only) Provides current elevation data like total gain/loss, steps ascended/descended, flights ascended/descended, and elevation rate.
                        bandClient.SensorManager.Altimeter.ReadingChanged += Altimeter_ReadingChanged;
                        await bandClient.SensorManager.Altimeter.StartReadingsAsync();
                    }

                    //Receive HeartRate data for a duration picked by the user, then stop the subscription.
                    int ingestDuration = 0;
                    switch (IngestDurationComboBox.SelectedIndex)
                    {
                        case 0:
                            ingestDuration = 1;
                            break;
                        case 1:
                            ingestDuration = 5;
                            break;
                        case 2:
                            ingestDuration = 10;
                            break;
                        case 3:
                            ingestDuration = 15;
                            break;

                        default:
                            ingestDuration = 1;
                            break;
                    }

                    await Task.Delay(TimeSpan.FromMinutes(ingestDuration));

                    await bandClient.SensorManager.HeartRate.StopReadingsAsync();
                    await bandClient.SensorManager.SkinTemperature.StopReadingsAsync();
                    await bandClient.SensorManager.Calories.StopReadingsAsync();
                    await bandClient.SensorManager.Distance.StopReadingsAsync();

                    if (bandClient.SensorManager.Pedometer.IsSupported) await bandClient.SensorManager.Pedometer.StopReadingsAsync();
                    if (bandClient.SensorManager.Barometer.IsSupported) await bandClient.SensorManager.Barometer.StopReadingsAsync();
                    if (bandClient.SensorManager.Gsr.IsSupported) await bandClient.SensorManager.Gsr.StopReadingsAsync();
                    if (bandClient.SensorManager.AmbientLight.IsSupported) await bandClient.SensorManager.AmbientLight.StopReadingsAsync();
                    if (bandClient.SensorManager.Altimeter.IsSupported) await bandClient.SensorManager.Altimeter.StopReadingsAsync();

                    StartButton.Content = "Start";
                    MicrosoftBandTextBlock.Text = string.Format("Done. {0} Microsoft Band telemetry were ingested.", samplesReceived);
                    this.ResetTelemetryReading();
                    return;

                }
            }
            catch (BandException ex)
            {
                // handle a Band connection exception } 
                System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + ex.ToString());
                MicrosoftBandTextBlock.Text = "Error: Unable to connect to Microsoft Band.";
            }
            return;

        }

        /// <summary>
        /// This is the 'getGeolocation' method
        /// </summary>
        async private void GetGeolocation()
        {
            try
            {
                // Get cancellation token
                _cts = new CancellationTokenSource();
                CancellationToken token = _cts.Token;

                // Carry out the operation
                Geoposition pos = await _geolocator.GetGeopositionAsync().AsTask(token);

                latitude = pos.Coordinate.Point.Position.Latitude;
                longitude = pos.Coordinate.Point.Position.Longitude;
            }
            catch (System.UnauthorizedAccessException)
            {
                //rootPage.NotifyUser("Disabled", NotifyType.StatusMessage);
            }
            catch (TaskCanceledException)
            {
                //rootPage.NotifyUser("Canceled", NotifyType.StatusMessage);
            }
            catch (Exception)
            {
                //rootPage.NotifyUser(ex.ToString(), NotifyType.ErrorMessage);
            }
            finally
            {
                _cts = null;
            }
        }

        private void Altimeter_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandAltimeterReading> e)
        {
            samplesReceived++;

            string fa = e.SensorReading.FlightsAscended.ToString();
            bandTelemetry.FlightsAscended = fa;
            ApplicationData.Current.LocalSettings.Values["FlightsAscended"] = fa;

            string fd = e.SensorReading.FlightsDescended.ToString();
            bandTelemetry.FlightsDescended = fa;
            ApplicationData.Current.LocalSettings.Values["FlightsDescended"] = fd;

            string r = e.SensorReading.Rate.ToString();
            bandTelemetry.AltimeterRate = r;
            ApplicationData.Current.LocalSettings.Values["AltimeterRate"] = r;

            string sg = e.SensorReading.SteppingGain.ToString();
            bandTelemetry.SteppingGain = sg;
            ApplicationData.Current.LocalSettings.Values["SteppingGain"] = sg;

            string sl = e.SensorReading.SteppingLoss.ToString();
            bandTelemetry.SteppingLoss = sl;
            ApplicationData.Current.LocalSettings.Values["SteppingLoss"] = sl;

            string sa = e.SensorReading.StepsAscended.ToString();
            bandTelemetry.StepsAscended = sa;
            ApplicationData.Current.LocalSettings.Values["StepsAscended"] = sa;

            string sd = e.SensorReading.StepsDescended.ToString();
            bandTelemetry.StepsDescended = sd;
            ApplicationData.Current.LocalSettings.Values["StepsDescended"] = sd;

            string tg = e.SensorReading.TotalGain.ToString();
            bandTelemetry.TotalGain = tg;
            ApplicationData.Current.LocalSettings.Values["TotalGain"] = tg;

            string tl = e.SensorReading.TotalLoss.ToString();
            bandTelemetry.TotalLoss = tl;
            ApplicationData.Current.LocalSettings.Values["TotalLoss"] = tl;


            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);

        }

        private void AmbientLight_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandAmbientLightReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.Brightness.ToString();
            bandTelemetry.Brightness = val;

            ApplicationData.Current.LocalSettings.Values["Brightness"] = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);

        }

        private void Gsr_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandGsrReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.Resistance.ToString();
            bandTelemetry.GsrResistance = val;

            ApplicationData.Current.LocalSettings.Values["GsrResistance"] = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);
        }

        private void Barometer_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandBarometerReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.AirPressure.ToString();
            bandTelemetry.AirPressure = val;
            ApplicationData.Current.LocalSettings.Values["AirPressure"] = val;

            string val2 = e.SensorReading.Temperature.ToString();
            bandTelemetry.Temperature = val2;
            ApplicationData.Current.LocalSettings.Values["Temperature"] = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);

        }

        private void Calories_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandCaloriesReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.Calories.ToString();
            bandTelemetry.Calories = val;

            ApplicationData.Current.LocalSettings.Values["Calories"] = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);

        }

        private void Distance_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandDistanceReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.TotalDistance.ToString();
            bandTelemetry.Distance = val;

            ApplicationData.Current.LocalSettings.Values["Distance"] = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);

        }

        private void SkinTemperature_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandSkinTemperatureReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.Temperature.ToString();
            bandTelemetry.SkinTemperature = val;
            bandTelemetry.Temperature = val;

            ApplicationData.Current.LocalSettings.Values["SkinTemperature"] = val;
            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);

        }

        private void Pedometer_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandPedometerReading> e)
        {
            samplesReceived++;

            string steps = e.SensorReading.TotalSteps.ToString();
            bandTelemetry.Pedometer = steps;

            ApplicationData.Current.LocalSettings.Values["Pedometer"] = steps;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);

        }

        /// <summary>
        /// This is the event handler for heart rate ReadingChanged events. It sends the heart rate reading to Azure IoT Hub
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HeartRate_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandHeartRateReading> e)
        {
            samplesReceived++;

            string hrm = e.SensorReading.HeartRate.ToString();
            bandTelemetry.HeartRate = hrm;
            bandTelemetry.Humidity = hrm; // using Humidity as placeholder because the current Remote Monitoring dashboard only shows 2 data points; humidity and temperature

            ApplicationData.Current.LocalSettings.Values["HeartRate"] = hrm;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);

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

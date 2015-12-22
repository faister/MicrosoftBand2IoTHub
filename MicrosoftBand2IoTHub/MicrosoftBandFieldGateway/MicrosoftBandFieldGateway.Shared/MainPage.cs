/*
    Copyright (c) 2015 faister

    The MIT License (MIT)

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.Devices.Geolocation;
using IoTHub;

#if MICROSOFT_BAND_SDK
using Microsoft.Band;
using Microsoft.Band.Sensors;
#else
using Microsoft.Band.Portable;
using Microsoft.Band.Portable.Sensors;
using System.Linq;
#endif

namespace MicrosoftBandFieldGateway
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    partial class MainPage
    {
        private App viewModel;

        // For IoT Hub communication
        private static IoTHubHttpServiceManager IoTHubHttpServiceManager;

        // For Microsoft Band
#if MICROSOFT_BAND_SDK
        private IBandClient bandClient;
#else
        private BandClient bandClient;
#endif
        private AppSettings AppSettings;
        private MicrosoftBandTelemetry bandTelemetry;
        public string FWVersion;
        public string HWVersion;
        public string ConnectedBandName;
        int samplesReceived = 0; // the number of HeartRate samples received

        // for geolocation
        private Geolocator _geolocator = null;
        private CancellationTokenSource _cts = null;
        private double latitude, longitude;
        private bool IsDeviceInfoUpdated = false;


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

#if MICROSOFT_BAND_SDK
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {

            string action = this.viewModel.ButtonContent;

            // Change UI captions
            if (action.Equals("Start"))
            {
                this.viewModel.ButtonContent = "Stop";
                this.viewModel.StatusMessage = "Searching for Band....";
            }
            else
            {
                if (bandClient != null)
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
                }

                this.viewModel.ButtonContent = "Start";
                this.viewModel.StatusMessage = string.Format("Done. {0} Microsoft Band telemetry were ingested.", samplesReceived);
                return;
            }

            if (IoTHubHttpServiceManager == null)
            {
                IoTHubHttpServiceManager = new IoTHubHttpServiceManager(AppSettings.IoTHubHostName, AppSettings.DeviceID, AppSettings.DeviceKey);
            }
            try
            {
                IBandInfo[] pairedBands = await BandClientManager.Instance.GetBandsAsync();
                if (pairedBands.Length < 1)
                {
                    this.viewModel.StatusMessage = "This sample app requires a Microsoft Band paired to your device. Also make sure that you have the latest firmware installed on your Band, as provided by the latest Microsoft Health app.";
                    return;
                }

                using (bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]))
                {
                    // do work after successful connect     
                    ConnectedBandName = pairedBands[0].Name;
                    this.viewModel.StatusMessage = "Connected: " + ConnectedBandName;

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
                        this.viewModel.StatusMessage = "Access to the heart rate sensor is denied.";
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
                        //ApplicationData.Current.LocalSettings.Values["AirPressure"] = "Band2 only";
                        // Subscribe to Barometer data. - (Microsoft Band 2 only) Provides the current raw air pressure in hPa (hectopascals) and raw temperature in degrees Celsius.
                        bandClient.SensorManager.Barometer.ReadingChanged += Barometer_ReadingChanged;
                        await bandClient.SensorManager.Barometer.StartReadingsAsync();
                    }
                    else
                        this.viewModel.AirPressure = "Band2 only";

                    if (bandClient.SensorManager.Gsr.IsSupported)
                    {
                        //ApplicationData.Current.LocalSettings.Values["GsrResistance"] = "Band2 only";
                        // Subscribe to Galvanic Skin Response. - (Microsoft Band 2 only) Provides the current skin resistance of the wearer in kohms.
                        bandClient.SensorManager.Gsr.ReadingChanged += Gsr_ReadingChanged;
                        await bandClient.SensorManager.Gsr.StartReadingsAsync();
                    }
                    else
                        this.viewModel.GsrResistance = "Band2 only";


                    if (bandClient.SensorManager.AmbientLight.IsSupported)
                    {
                        //                        ApplicationData.Current.LocalSettings.Values["Brightness"] = "Band2 only";

                        // Subscribe to Ambient Light - (Microsoft Band 2 only) Provides the current light intensity (illuminance) in lux (Lumes per sq. meter).
                        bandClient.SensorManager.AmbientLight.ReadingChanged += AmbientLight_ReadingChanged;
                        await bandClient.SensorManager.AmbientLight.StartReadingsAsync();
                    }
                    else
                        this.viewModel.Brightness = "Band2 only";


                    if (bandClient.SensorManager.Altimeter.IsSupported)
                    {
                        //ApplicationData.Current.LocalSettings.Values["AltimeterRate"] = "Band2 only";
                        // Subscribe to Altimeter - (Microsoft Band 2 only) Provides current elevation data like total gain/loss, steps ascended/descended, flights ascended/descended, and elevation rate.
                        bandClient.SensorManager.Altimeter.ReadingChanged += Altimeter_ReadingChanged;
                        await bandClient.SensorManager.Altimeter.StartReadingsAsync();
                    }
                    else
                        this.viewModel.AltimeterRate = "Band2 only";

                    //Receive HeartRate data for a duration picked by the user, then stop the subscription.
                    int ingestDuration = 0;
                    switch (this.viewModel.IngestDuration)
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

                    this.viewModel.ButtonContent = "Start";
                    this.viewModel.StatusMessage = string.Format("Done. {0} Microsoft Band telemetry were ingested.", samplesReceived);
                    //this.ResetTelemetryReading();
                    return;

                }
            }
            catch (BandException ex)
            {
                // handle a Band connection exception } 
                System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + ex.ToString());
                this.viewModel.StatusMessage = "Error: Unable to communicate with Microsoft Band.";
            }
            return;
        }

        private void Altimeter_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandAltimeterReading> e)
        {
            samplesReceived++;

            string fa = e.SensorReading.FlightsAscended.ToString();
            bandTelemetry.FlightsAscended = fa;
            //ApplicationData.Current.LocalSettings.Values["FlightsAscended"] = fa;

            string fd = e.SensorReading.FlightsDescended.ToString();
            bandTelemetry.FlightsDescended = fa;
            //ApplicationData.Current.LocalSettings.Values["FlightsDescended"] = fd;

            string r = e.SensorReading.Rate.ToString();
            bandTelemetry.AltimeterRate = r;
            //ApplicationData.Current.LocalSettings.Values["AltimeterRate"] = r;
            this.viewModel.AltimeterRate = r;

            string sg = e.SensorReading.SteppingGain.ToString();
            bandTelemetry.SteppingGain = sg;
            //ApplicationData.Current.LocalSettings.Values["SteppingGain"] = sg;

            string sl = e.SensorReading.SteppingLoss.ToString();
            bandTelemetry.SteppingLoss = sl;
            //ApplicationData.Current.LocalSettings.Values["SteppingLoss"] = sl;

            string sa = e.SensorReading.StepsAscended.ToString();
            bandTelemetry.StepsAscended = sa;
            //ApplicationData.Current.LocalSettings.Values["StepsAscended"] = sa;

            string sd = e.SensorReading.StepsDescended.ToString();
            bandTelemetry.StepsDescended = sd;
            //ApplicationData.Current.LocalSettings.Values["StepsDescended"] = sd;

            string tg = e.SensorReading.TotalGain.ToString();
            bandTelemetry.TotalGain = tg;
            //ApplicationData.Current.LocalSettings.Values["TotalGain"] = tg;

            string tl = e.SensorReading.TotalLoss.ToString();
            bandTelemetry.TotalLoss = tl;
            //ApplicationData.Current.LocalSettings.Values["TotalLoss"] = tl;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);
        }

        private void AmbientLight_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandAmbientLightReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.Brightness.ToString();
            bandTelemetry.Brightness = val;

            //ApplicationData.Current.LocalSettings.Values["Brightness"] = val;
            this.viewModel.Brightness = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);

        }

        private void Gsr_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandGsrReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.Resistance.ToString();
            bandTelemetry.GsrResistance = val;

            //ApplicationData.Current.LocalSettings.Values["GsrResistance"] = val;
            this.viewModel.GsrResistance = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);
        }

        private void Barometer_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandBarometerReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.AirPressure.ToString();
            bandTelemetry.AirPressure = val;
            //ApplicationData.Current.LocalSettings.Values["AirPressure"] = val;
            this.viewModel.AirPressure = val;

            string val2 = e.SensorReading.Temperature.ToString();
            bandTelemetry.Temperature = val2;
            //ApplicationData.Current.LocalSettings.Values["Temperature"] = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);
        }

        private void Calories_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandCaloriesReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.Calories.ToString();
            bandTelemetry.Calories = val;

            //ApplicationData.Current.LocalSettings.Values["Calories"] = val;
            this.viewModel.Calories = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);
        }

        private void Distance_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandDistanceReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.TotalDistance.ToString();
            bandTelemetry.Distance = val;

            //ApplicationData.Current.LocalSettings.Values["Distance"] = val;
            this.viewModel.Distance = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);
        }

        private void SkinTemperature_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandSkinTemperatureReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.Temperature.ToString();
            bandTelemetry.SkinTemperature = val;
            bandTelemetry.Temperature = val;

            //ApplicationData.Current.LocalSettings.Values["SkinTemperature"] = val;
            this.viewModel.SkinTemperature = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);
        }

        private void Pedometer_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandPedometerReading> e)
        {
            samplesReceived++;

            string steps = e.SensorReading.TotalSteps.ToString();
            bandTelemetry.Pedometer = steps;

            //ApplicationData.Current.LocalSettings.Values["Pedometer"] = steps;
            this.viewModel.Pedometer = steps;

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

            //ApplicationData.Current.LocalSettings.Values["HeartRate"] = hrm;
            this.viewModel.HeartRate = hrm;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);
        }

#else
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {

            string action = this.viewModel.ButtonContent;

            // Change UI captions
            if (action.Equals("Start"))
            {
                this.viewModel.ButtonContent = "Stop";
                this.viewModel.StatusMessage = "Searching for Band....";
            }
            else
            {
                if (bandClient != null)
                {
                    await bandClient.SensorManager.HeartRate.StopReadingsAsync();
                    await bandClient.SensorManager.SkinTemperature.StopReadingsAsync();
                    await bandClient.SensorManager.Calories.StopReadingsAsync();
                    await bandClient.SensorManager.Distance.StopReadingsAsync();
                    await bandClient.SensorManager.Pedometer.StopReadingsAsync();
                    if (bandClient.SensorManager.Barometer != null) await bandClient.SensorManager.Barometer.StopReadingsAsync();
                    if (bandClient.SensorManager.Gsr != null) await bandClient.SensorManager.Gsr.StopReadingsAsync();
                    if (bandClient.SensorManager.AmbientLight != null) await bandClient.SensorManager.AmbientLight.StopReadingsAsync();
                    if (bandClient.SensorManager.Altimeter != null) await bandClient.SensorManager.Altimeter.StopReadingsAsync();
                }

                this.viewModel.ButtonContent = "Start";
                this.viewModel.StatusMessage = string.Format("Done. {0} Microsoft Band telemetry were ingested.", samplesReceived);
                return;
            }

            if (IoTHubHttpServiceManager == null)
            {
                IoTHubHttpServiceManager = new IoTHubHttpServiceManager(AppSettings.IoTHubHostName, AppSettings.DeviceID, AppSettings.DeviceKey);
            }
            try
            {
                var bandClientManager = BandClientManager.Instance;
                // query the service for paired devices
                var pairedBands = await bandClientManager.GetPairedBandsAsync();
                // connect to the first device
                var bandInfo = pairedBands.FirstOrDefault();
                if (bandInfo == null)
                {
                    this.viewModel.StatusMessage = "This sample app requires a Microsoft Band paired to your device. Also make sure that you have the latest firmware installed on your Band, as provided by the latest Microsoft Health app.";
                    return;
                }

                bandClient = await bandClientManager.ConnectAsync(bandInfo);
                    // do work after successful connect     
                    ConnectedBandName = bandInfo.Name;
                    this.viewModel.StatusMessage = "Connected: " + ConnectedBandName;

                    FWVersion = await bandClient.GetFirmwareVersionAsync();
                    HWVersion = await bandClient.GetHardwareVersionAsync();

                bool heartRateConsentGranted = false;

                // check current user heart rate consent 
                if (bandClient.SensorManager.HeartRate.UserConsented == UserConsent.Unspecified)
                {
                    // user hasn’t consented, request consent  
                    await bandClient.SensorManager.HeartRate.RequestUserConsent();
                }


                    if (!heartRateConsentGranted)
                    {
                        this.viewModel.StatusMessage = "Access to the heart rate sensor is denied.";
                        return;
                    }

                    // Subscribe to HeartRate data.
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

                        // Subscribe to Altimeter data.
                        bandClient.SensorManager.Pedometer.ReadingChanged += Pedometer_ReadingChanged;
                        await bandClient.SensorManager.Pedometer.StartReadingsAsync();

                    if (bandClient.SensorManager.Barometer != null)
                    {
                        // Subscribe to Barometer data. - (Microsoft Band 2 only) Provides the current raw air pressure in hPa (hectopascals) and raw temperature in degrees Celsius.
                        bandClient.SensorManager.Barometer.ReadingChanged += Barometer_ReadingChanged;
                        await bandClient.SensorManager.Barometer.StartReadingsAsync();
                    }
                    else
                        this.viewModel.AirPressure = "Band2 only";

                    if (bandClient.SensorManager.Gsr != null)
                    {
                        // Subscribe to Galvanic Skin Response. - (Microsoft Band 2 only) Provides the current skin resistance of the wearer in kohms.
                        bandClient.SensorManager.Gsr.ReadingChanged += Gsr_ReadingChanged;
                        await bandClient.SensorManager.Gsr.StartReadingsAsync();
                    }
                    else
                        this.viewModel.GsrResistance = "Band2 only";


                    if (bandClient.SensorManager.AmbientLight != null)
                    {
                        // Subscribe to Ambient Light - (Microsoft Band 2 only) Provides the current light intensity (illuminance) in lux (Lumes per sq. meter).
                        bandClient.SensorManager.AmbientLight.ReadingChanged += AmbientLight_ReadingChanged;
                        await bandClient.SensorManager.AmbientLight.StartReadingsAsync();
                    }
                    else
                        this.viewModel.Brightness = "Band2 only";


                    if (bandClient.SensorManager.Altimeter != null)
                    {
                        // Subscribe to Altimeter - (Microsoft Band 2 only) Provides current elevation data like total gain/loss, steps ascended/descended, flights ascended/descended, and elevation rate.
                        bandClient.SensorManager.Altimeter.ReadingChanged += Altimeter_ReadingChanged;
                        await bandClient.SensorManager.Altimeter.StartReadingsAsync();
                    }
                    else
                        this.viewModel.AltimeterRate = "Band2 only";

                    //Receive HeartRate data for a duration picked by the user, then stop the subscription.
                    int ingestDuration = 0;
                    switch (this.viewModel.IngestDuration)
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
                    await bandClient.SensorManager.Pedometer.StopReadingsAsync();

                    if (bandClient.SensorManager.Barometer != null) await bandClient.SensorManager.Barometer.StopReadingsAsync();
                    if (bandClient.SensorManager.Gsr != null) await bandClient.SensorManager.Gsr.StopReadingsAsync();
                    if (bandClient.SensorManager.AmbientLight != null) await bandClient.SensorManager.AmbientLight.StopReadingsAsync();
                    if (bandClient.SensorManager.Altimeter != null) await bandClient.SensorManager.Altimeter.StopReadingsAsync();

                    this.viewModel.ButtonContent = "Start";
                    this.viewModel.StatusMessage = string.Format("Done. {0} Microsoft Band telemetry were ingested.", samplesReceived);
                    return;


            }
            catch (Exception ex)
            {
                // handle a Band connection exception } 
                System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + ex.ToString());
                this.viewModel.StatusMessage = "Error: Unable to communicate with Microsoft Band.";
            }
            return;
        }

        private void Altimeter_ReadingChanged(object sender, BandSensorReadingEventArgs<BandAltimeterReading> e)
        {
            samplesReceived++;

            string fa = e.SensorReading.FlightsAscended.ToString();
            bandTelemetry.FlightsAscended = fa;
            //ApplicationData.Current.LocalSettings.Values["FlightsAscended"] = fa;

            string fd = e.SensorReading.FlightsDescended.ToString();
            bandTelemetry.FlightsDescended = fa;
            //ApplicationData.Current.LocalSettings.Values["FlightsDescended"] = fd;

            string r = e.SensorReading.Rate.ToString();
            bandTelemetry.AltimeterRate = r;
            //ApplicationData.Current.LocalSettings.Values["AltimeterRate"] = r;
            this.viewModel.AltimeterRate = r;

            string sg = e.SensorReading.SteppingGain.ToString();
            bandTelemetry.SteppingGain = sg;
            //ApplicationData.Current.LocalSettings.Values["SteppingGain"] = sg;

            string sl = e.SensorReading.SteppingLoss.ToString();
            bandTelemetry.SteppingLoss = sl;
            //ApplicationData.Current.LocalSettings.Values["SteppingLoss"] = sl;

            string sa = e.SensorReading.StepsAscended.ToString();
            bandTelemetry.StepsAscended = sa;
            //ApplicationData.Current.LocalSettings.Values["StepsAscended"] = sa;

            string sd = e.SensorReading.StepsDescended.ToString();
            bandTelemetry.StepsDescended = sd;
            //ApplicationData.Current.LocalSettings.Values["StepsDescended"] = sd;

            string tg = e.SensorReading.TotalGain.ToString();
            bandTelemetry.TotalGain = tg;
            //ApplicationData.Current.LocalSettings.Values["TotalGain"] = tg;

            string tl = e.SensorReading.TotalLoss.ToString();
            bandTelemetry.TotalLoss = tl;
            //ApplicationData.Current.LocalSettings.Values["TotalLoss"] = tl;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);

        }

        private void AmbientLight_ReadingChanged(object sender, BandSensorReadingEventArgs<BandAmbientLightReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.Brightness.ToString();
            bandTelemetry.Brightness = val;

            //ApplicationData.Current.LocalSettings.Values["Brightness"] = val;
            this.viewModel.Brightness = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);

        }

        private void Gsr_ReadingChanged(object sender, BandSensorReadingEventArgs<BandGsrReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.Resistance.ToString();
            bandTelemetry.GsrResistance = val;

            //ApplicationData.Current.LocalSettings.Values["GsrResistance"] = val;
            this.viewModel.GsrResistance = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);
        }

        private void Barometer_ReadingChanged(object sender, BandSensorReadingEventArgs<BandBarometerReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.AirPressure.ToString();
            bandTelemetry.AirPressure = val;
            //ApplicationData.Current.LocalSettings.Values["AirPressure"] = val;
            this.viewModel.AirPressure = val;

            string val2 = e.SensorReading.Temperature.ToString();
            bandTelemetry.Temperature = val2;
            //ApplicationData.Current.LocalSettings.Values["Temperature"] = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);
        }

        private void Calories_ReadingChanged(object sender, BandSensorReadingEventArgs<BandCaloriesReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.Calories.ToString();
            bandTelemetry.Calories = val;

            //ApplicationData.Current.LocalSettings.Values["Calories"] = val;
            this.viewModel.Calories = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);
        }

        private void Distance_ReadingChanged(object sender, BandSensorReadingEventArgs<BandDistanceReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.TotalDistance.ToString();
            bandTelemetry.Distance = val;

            //ApplicationData.Current.LocalSettings.Values["Distance"] = val;
            this.viewModel.Distance = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);
        }

        private void SkinTemperature_ReadingChanged(object sender, BandSensorReadingEventArgs<BandSkinTemperatureReading> e)
        {
            samplesReceived++;

            string val = e.SensorReading.Temperature.ToString();
            bandTelemetry.SkinTemperature = val;
            bandTelemetry.Temperature = val;

            //ApplicationData.Current.LocalSettings.Values["SkinTemperature"] = val;
            this.viewModel.SkinTemperature = val;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);
        }

        private void Pedometer_ReadingChanged(object sender, BandSensorReadingEventArgs<BandPedometerReading> e)
        {
            samplesReceived++;

            string steps = e.SensorReading.TotalSteps.ToString();
            bandTelemetry.Pedometer = steps;

            //ApplicationData.Current.LocalSettings.Values["Pedometer"] = steps;
            this.viewModel.Pedometer = steps;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);
        }

        /// <summary>
        /// This is the event handler for heart rate ReadingChanged events. It sends the heart rate reading to Azure IoT Hub
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HeartRate_ReadingChanged(object sender, BandSensorReadingEventArgs<BandHeartRateReading> e)
        {
            samplesReceived++;

            string hrm = e.SensorReading.HeartRate.ToString();
            bandTelemetry.HeartRate = hrm;
            bandTelemetry.Humidity = hrm; // using Humidity as placeholder because the current Remote Monitoring dashboard only shows 2 data points; humidity and temperature

            //ApplicationData.Current.LocalSettings.Values["HeartRate"] = hrm;
            this.viewModel.HeartRate = hrm;

            IoTHubHttpServiceManager.SendIoTHubMessage(bandTelemetry);
        }

#endif

    }
}

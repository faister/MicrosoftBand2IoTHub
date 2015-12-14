using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Band;
using Microsoft.Band.Sensors;
using IoTHub;
using System.Threading.Tasks;
using Windows.Storage;

namespace MicrosoftBandFieldGateway
{
    public class BandServiceManager
    {
        private static BandServiceManager bandServiceManager;
        private static IBandClient bandClient;

        public static string FWVersion;
        public static string HWVersion;
        public static string ConnectedBandName;

        private IBandInfo[] pairedBands;
        private MicrosoftBandTelemetry bandTelemetry;

        public static async Task<string> Initialize()
        {
            if (bandServiceManager == null)
                bandServiceManager = new BandServiceManager();

            try
            {
                bandServiceManager.pairedBands = await BandClientManager.Instance.GetBandsAsync();
                if (bandServiceManager.pairedBands.Length < 1)
                {
                    ApplicationData.Current.LocalSettings.Values["StatusMessage"] = "This sample app requires a Microsoft Band paired to your device. Also make sure that you have the latest firmware installed on your Band, as provided by the latest Microsoft Health app.";
                    return null;
                }

                using (bandClient = await BandClientManager.Instance.ConnectAsync(bandServiceManager.pairedBands[0]))
                {
                    // do work after successful connect     
                    ConnectedBandName = bandServiceManager.pairedBands[0].Name;
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
                        ApplicationData.Current.LocalSettings.Values["StatusMessage"] = "Access to the heart rate sensor is denied.";
                        return null;
                    }

                    // hook up to the Heartrate sensor ReadingChanged event 
                    bandClient.SensorManager.HeartRate.ReadingChanged += HeartRate_ReadingChanged;
                    await bandClient.SensorManager.HeartRate.StartReadingsAsync();
                }
            }
            catch (BandException ex)
            {
                // handle a Band connection exception } 
                System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + ex.ToString());
                throw ex;
            }
            return ConnectedBandName;
        }

        public BandServiceManager()
        {
            this.bandTelemetry = new MicrosoftBandTelemetry();
        }


        static public async void StartSensorTelemetryAsync()
        {
            if (bandServiceManager == null)
                throw new Exception("Exception: BandServiceManager is not initialized");
            try
            {
                // get a list of available reporting intervals 
                //IEnumerable<TimeSpan> supportedHeartBeatReportingIntervals = bandServiceManager.bandClient.SensorManager.HeartRate.SupportedReportingIntervals;

                // set the reporting interval 
                //bandServiceManager.bandClient.SensorManager.HeartRate.ReportingInterval = supportedHeartBeatReportingIntervals.GetEnumerator().Current;


            }
            catch (BandException ex)
            {
                // handle a Band connection exception } 
                System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + ex.ToString());
                throw ex;
            }
        }


        /// <summary>
        /// This is the event handler for heart rate ReadingChanged events. It sends the heart rate reading to Azure IoT Hub
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static public void HeartRate_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandHeartRateReading> e)
        {
            //_sampleCount++;
            //var heartRate = e.SensorReading.HeartRate;
            //var HRQuality = e.SensorReading.Quality;
            //var HrTimestamp = e.SensorReading.Timestamp;

            string hrm = GetStringValue(e.SensorReading);
            bandServiceManager.bandTelemetry.HeartRate = hrm;
            ApplicationData.Current.LocalSettings.Values["HeartRate"] = hrm;


            IoTHubServiceManager.SendMicrosoftBandTelemetry(bandServiceManager.bandTelemetry);
        }

        private static string GetStringValue(IBandSensorReading value)
        {
            return value
                .ToString()
                .Replace(", ", Environment.NewLine)
                .Replace("=", " = ");
        }


    }
}

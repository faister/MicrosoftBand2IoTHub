using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using System.Globalization;
using Windows.System.Profile;
using Windows.Storage.Streams;
using Windows.Devices.Geolocation;
using System.Threading;
using System.Diagnostics;
using MicrosoftBandFieldGateway;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;

namespace IoTHub
{
    public class IoTHubHttpServiceManager
    {
        const char Base64Padding = '=';
        static readonly HashSet<char> base64Table = new HashSet<char>{'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O',
                                                                      'P','Q','R','S','T','U','V','W','X','Y','Z','a','b','c','d',
                                                                      'e','f','g','h','i','j','k','l','m','n','o','p','q','r','s',
                                                                      't','u','v','w','x','y','z','0','1','2','3','4','5','6','7',
                                                                      '8','9','+','/' };
        private static JsonSerializer serializer = null;

        //private AppSettings _appSettings;
        private string connectionString;


        // For IoT Hub
        object publishLock = new object();
        static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromSeconds(60);
        System.Net.Http.HttpClient httpClientObj = null;
        private string HostName;
        private string DeviceID;
        private string SharedAccessKey;
        private HttpClient httpClient;
        private Uri uri;
        private string sas;

        private double Latitude = 0;
        private double Longitude = 0;
        private string FWVersion;
        private string HWVersion;

        /// <summary>
        /// Constructor for initializing the HTTP/1 connection with Azure IoT Hub
        /// </summary>
        public IoTHubHttpServiceManager(string hn, string did, string key)
        {
            this.HostName = hn;
            this.DeviceID = did;
            this.SharedAccessKey = key;

            try
            {
                // MANDATORY to have ?api-version=2015-08-15-preview as the query string otherwise the IoT Hub HTTPS D2C endpoint would throw a HTTP error with StatusCode: 400, ReasonPhrase: 'Bad Request'
                string requestUri = String.Format("/devices/{0}/messages/events?api-version=2015-08-15-preview", DeviceID);
                string sr = String.Format("{0}/devices/{1}", this.HostName, this.DeviceID);

                // Note: The SAS Token is set to expire after 5 minutes so as to limit the telemetry being sent to the IoT Hub
                this.sas = BuildSignature(null, this.SharedAccessKey, sr, TimeSpan.FromMinutes(60));
                this.uri = new Uri(String.Format("https://{0}{1}", this.HostName, requestUri));

                this.httpClient = new HttpClient();
                this.httpClient.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("SharedAccessSignature", sas);

                this.UpdateDeviceInfo();
            }
            catch (Exception e)
            {
                // do something
                Debug.WriteLine("Exception when initializing connection to IoT Hub:" + e.Message);

            }
        }


        /// <summary>
        /// Sends Device-to-Cloud Message to IoT Hub. Defaults to HTTPS protocol 
        /// </summary>
        /// <param name="body"></param>
        public async void SendIoTHubMessage(string body)
        {
            using (var msg = new HttpRequestMessage(HttpMethod.Post, this.uri))
            {
                if (this.uri == null) return;
                HttpResponseMessage responseMsg;
                try
                {
                    msg.Content = new HttpStringContent(body);
                    //msg.Headers.Add()
                    msg.Content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/json");

                    responseMsg = await this.httpClient.SendRequestAsync(msg);
                    if (responseMsg == null)
                    {
                        throw new InvalidOperationException("The response message was null when executing operation POST telemetry to IoT Hub");
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception when sending message:" + e.Message);
                }
            }
        }

        /// <summary>
        /// Sends Device-to-Cloud Message to IoT Hub. Defaults to HTTPS protocol 
        /// </summary>
        /// <param name="body"></param>
        public async void SendIoTHubMessage(MicrosoftBandTelemetry bandTelemetry)
        {
            string eventData;

            if (String.IsNullOrEmpty(bandTelemetry.DeviceId))
                bandTelemetry.DeviceId = this.DeviceID;

            if (serializer == null)
            {
                serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
            }


            using (StringWriter sw = new StringWriter())
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, bandTelemetry);
                eventData = sw.ToString();
            }
            this.SendIoTHubMessage(eventData);
        }

        public void UpdateDeviceInfo(double lat, double longi, string fwVersion, string hwVersion)
        {
            this.Latitude = lat;
            this.Longitude = longi;
            this.FWVersion = fwVersion;
            this.HWVersion = hwVersion;
            this.UpdateDeviceInfo();

        }

        private void UpdateDeviceInfo()
        {
            try
            {
                // The following code is needed when this Device wakes up and send its updated device properties to the IoT Hub
                string createdDateTime = DateTime.UtcNow.ToString("o");

                // Device Info for MS Band
                string deviceInfo = "{\"DeviceProperties\":{\"DeviceID\":\"" + this.DeviceID + "\",\"HubEnabledState\":true,\"CreatedTime\":\"" + createdDateTime + "\",\"DeviceState\":\"normal\",\"UpdatedTime\":null,\"Manufacturer\":\"Microsoft Corp\",\"ModelNumber\":\"" + this.HWVersion + "\",\"SerialNumber\":\"N/A\",\"FirmwareVersion\":\""+ this.FWVersion + "\",\"Platform\":\"Windows Phone 8.1\",\"Processor\":\"ARM\",\"InstalledRAM\":\"4GB\",\"Latitude\":" + this.Latitude.ToString() + ",\"Longitude\":" + this.Longitude.ToString() + "},\"Commands\":[{\"Name\":\"PingDevice\",\"Parameters\":null},{\"Name\":\"StartTelemetry\",\"Parameters\":null},{\"Name\":\"StopTelemetry\",\"Parameters\":null},{\"Name\":\"ChangeSetPointTemp\",\"Parameters\":[{\"Name\":\"SetPointTemp\",\"Type\":\"double\"}]},{\"Name\":\"DiagnosticTelemetry\",\"Parameters\":[{\"Name\":\"Active\",\"Type\":\"boolean\"}]},{\"Name\":\"ChangeDeviceState\",\"Parameters\":[{\"Name\":\"DeviceState\",\"Type\":\"string\"}]}],\"CommandHistory\":[],\"IsSimulatedDevice\":false,\"Version\":\"1.0\",\"ObjectType\":\"DeviceInfo\"}";
                this.SendIoTHubMessage(deviceInfo);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception when sending Device Info to IoT Hub:" + e.Message);
            }
        }

        static string BuildSignature(string keyName, string key, string target, TimeSpan timeToLive)
        {
            string expiresOn = BuildExpiresOn(timeToLive);
            string audience = WebUtility.UrlEncode(target);
            List<string> fields = new List<string>();
            fields.Add(audience);
            fields.Add(expiresOn);

            // Example string to be signed:
            // dh://myiothub.azure-devices.net/a/b/c?myvalue1=a
            // <Value for ExpiresOn>

            string signature = Sign(string.Join("\n", fields), key);

            // Example returned string:
            // SharedAccessSignature sr=ENCODED(dh://myiothub.azure-devices.net/a/b/c?myvalue1=a)&sig=<Signature>&se=<ExpiresOnValue>[&skn=<KeyName>]

            var buffer = new StringBuilder();
            buffer.AppendFormat(CultureInfo.InvariantCulture, "sr={0}&sig={1}&se={2}",
                                audience,
                                WebUtility.UrlEncode(signature),
                                WebUtility.UrlEncode(expiresOn));

            if (!string.IsNullOrEmpty(keyName))
            {
                buffer.AppendFormat(CultureInfo.InvariantCulture, "&{0}={1}",
                    "skn", WebUtility.UrlEncode(keyName));
            }

            return buffer.ToString();
        }

        static string BuildExpiresOn(TimeSpan timeToLive)
        {
            DateTime expiresOn = DateTime.UtcNow.Add(timeToLive);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            long seconds = Convert.ToInt64(secondsFromBaseTime.TotalSeconds, CultureInfo.InvariantCulture);
            return Convert.ToString(seconds, CultureInfo.InvariantCulture);
        }


        static string Sign(string requestString, string key)
        {
            if (!IsBase64String(key))
                throw new ArgumentException("The SharedAccessKey of the device is not a Base64String");

            var algo = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha256);
            var keyMaterial = Convert.FromBase64String(key).AsBuffer();
            var hash = algo.CreateHash(keyMaterial);
            hash.Append(CryptographicBuffer.ConvertStringToBinary(requestString, BinaryStringEncoding.Utf8));

            var sign = CryptographicBuffer.EncodeToBase64String(hash.GetValueAndReset());
            return sign;
        }

        public static bool IsBase64String(string value)
        {
            value = value.Replace("\r", string.Empty).Replace("\n", string.Empty);

            if (value.Length == 0 || (value.Length % 4) != 0)
            {
                return false;
            }

            var lengthNoPadding = value.Length;
            value = value.TrimEnd(Base64Padding);
            var lengthPadding = value.Length;

            if ((lengthNoPadding - lengthPadding) > 2)
            {
                return false;
            }

            foreach (char c in value)
            {
                if (!base64Table.Contains(c))
                {
                    return false;
                }
            }
            return true;
        }

    }
}

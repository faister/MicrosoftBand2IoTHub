using Amqp;
using Amqp.Framing;
using System;
using System.Diagnostics;
using Windows.System.Threading;
using System.Text;
using System.Threading.Tasks;
using MicrosoftBandFieldGateway;
using Newtonsoft.Json;
using System.IO;
#if NETMF
using Microsoft.SPOT;
#endif

namespace IoTHub
{
    class IoTHubServiceManager
    {
        private const string HOST = "faisterremote.azure-devices.net";
        private const int PORT = 5671;
        private const string DEVICE_ID = "IgniteBand";
        private const string DEVICE_KEY = "RtBb9H28DX/TsgTcwUFpZg==";

        private static Address address;
        private static Connection connection;
        private static Session session;
        private static double latitude = -33.793152;
        private static double longitude = 151.287734;

        private static JsonSerializer serializer = null;

        public static void Initialize()
        {
            Amqp.Trace.TraceLevel = Amqp.TraceLevel.Frame | Amqp.TraceLevel.Verbose;
#if NETMF
            Amqp.Trace.TraceListener = (f, a) => Debug.Print(DateTime.Now.ToString("[hh:ss.fff]") + " " + Fx.Format(f, a));
#else
            Amqp.Trace.TraceListener = (f, a) => System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + Fx.Format(f, a));
#endif
            address = new Address(HOST, PORT, null, null);
            connection = new Connection(address);

            string audience = Fx.Format("{0}/devices/{1}", HOST, DEVICE_ID);
            string resourceUri = Fx.Format("{0}/devices/{1}", HOST, DEVICE_ID);
            
            string sasToken = GetSharedAccessSignature(null, DEVICE_KEY, resourceUri, new TimeSpan(1, 0, 0));
            bool cbs = PutCbsToken(connection, HOST, sasToken, audience);

            if (cbs)
            {
                session = new Session(connection);

                // Send DeviceInfo JSON to initialize this Microsoft Band
                UpdateDeviceInfo();

                ReceiveCommandsAsync().Wait();
            }


            //session.Close();
            //connection.Close();
        }

        // Fai: to send DeviceInfo update to remote monitoring solution
        static void UpdateDeviceInfo()
        {
            string createdDateTime = DateTime.UtcNow.ToString("o");
            //string deviceInfo = "{\"DeviceProperties\":{\"DeviceID\":\"" + deviceID + "\",\"HubEnabledState\":true,\"CreatedTime\":\"" + createdDateTime + "\",\"DeviceState\":\"normal\",\"UpdatedTime\":null,\"Manufacturer\":\"Texas Instruments\",\"ModelNumber\":\"CC2541\",\"SerialNumber\":\"n/a\",\"FirmwareVersion\":\"Rev. B\",\"Platform\":\"Windows Phone 8.1\",\"Processor\":\"TPS62730\",\"InstalledRAM\":\"n/a\",\"Latitude\":" + latitude.ToString() + ",\"Longitude\":" + longitude.ToString() + "},\"Commands\":[{\"Name\":\"StartTelemetry\",\"Parameters\":null},{\"Name\":\"StopTelemetry\",\"Parameters\":null}]},{\"Name\":\"DiagnosticTelemetry\",\"Parameters\":[{\"Name\":\"Active\",\"Type\":\"boolean\"}]},{\"Name\":\"ChangeDeviceState\",\"Parameters\":[{\"Name\":\"DeviceState\",\"Type\":\"string\"}]}],\"CommandHistory\":[],\"IsSimulatedDevice\":true,\"Version\":\"1.0\",\"ObjectType\":\"DeviceInfo\"}";
            // FaisterSensorTag string deviceInfo = "{\"DeviceProperties\":{\"DeviceID\":\"" + deviceID + "\",\"HubEnabledState\":true,\"CreatedTime\":\"" + createdDateTime + "\",\"DeviceState\":\"normal\",\"UpdatedTime\":null,\"Manufacturer\":\"Texas Instruments\",\"ModelNumber\":\"CC2541\",\"SerialNumber\":\"n/a\",\"FirmwareVersion\":\"Rev. B\",\"Platform\":\"Windows Phone 8.1\",\"Processor\":\"n/a\",\"InstalledRAM\":\"n/a\",\"Latitude\":" + latitude.ToString() + ",\"Longitude\":" + longitude.ToString() + "},\"Commands\":[{\"Name\":\"PingDevice\",\"Parameters\":null},{\"Name\":\"StartTelemetry\",\"Parameters\":null},{\"Name\":\"StopTelemetry\",\"Parameters\":null},{\"Name\":\"ChangeSetPointTemp\",\"Parameters\":[{\"Name\":\"SetPointTemp\",\"Type\":\"double\"}]},{\"Name\":\"DiagnosticTelemetry\",\"Parameters\":[{\"Name\":\"Active\",\"Type\":\"boolean\"}]},{\"Name\":\"ChangeDeviceState\",\"Parameters\":[{\"Name\":\"DeviceState\",\"Type\":\"string\"}]}],\"CommandHistory\":[],\"IsSimulatedDevice\":true,\"Version\":\"1.0\",\"ObjectType\":\"DeviceInfo\"}";

            // Device Info for NodeMCU
            //string deviceInfo = "{\"DeviceProperties\":{\"DeviceID\":\"" + DEVICE_ID + "\",\"HubEnabledState\":true,\"CreatedTime\":\"" + createdDateTime + "\",\"DeviceState\":\"normal\",\"UpdatedTime\":null,\"Manufacturer\":\"ESP8266 Opensource Community\",\"ModelNumber\":\"ESP8266\",\"SerialNumber\":\"n/a\",\"FirmwareVersion\":\"1.0 (ESP-12E Module)\",\"Platform\":\"XTOS\",\"Processor\":\"ESP8266(LX106)\",\"InstalledRAM\":\"1,044,464 bytes\",\"Latitude\":" + latitude.ToString() + ",\"Longitude\":" + longitude.ToString() + "},\"Commands\":[{\"Name\":\"PingDevice\",\"Parameters\":null},{\"Name\":\"StartTelemetry\",\"Parameters\":null},{\"Name\":\"StopTelemetry\",\"Parameters\":null},{\"Name\":\"ChangeSetPointTemp\",\"Parameters\":[{\"Name\":\"SetPointTemp\",\"Type\":\"double\"}]},{\"Name\":\"DiagnosticTelemetry\",\"Parameters\":[{\"Name\":\"Active\",\"Type\":\"boolean\"}]},{\"Name\":\"ChangeDeviceState\",\"Parameters\":[{\"Name\":\"DeviceState\",\"Type\":\"string\"}]}],\"CommandHistory\":[],\"IsSimulatedDevice\":false,\"Version\":\"1.0\",\"ObjectType\":\"DeviceInfo\"}";

            //string deviceInfo = "{\"DeviceProperties\":{\"DeviceID\":\"" + deviceID + "\",\"HubEnabledState\":true,\"CreatedTime\":\"" + createdDateTime + "\",\"DeviceState\":\"normal\",\"UpdatedTime\":null,\"Manufacturer\":\"Azure IoT RedCarpet\",\"ModelNumber\":\"RedCarpet\",\"SerialNumber\":\"n/a\",\"FirmwareVersion\":\"1.0\",\"Platform\":\"Windows\",\"Processor\":\"ARM Cortex-A53\",\"InstalledRAM\":\"1 GB\",\"Latitude\":" + latitude.ToString() + ",\"Longitude\":" + longitude.ToString() + "},\"Commands\":[{\"Name\":\"PingDevice\",\"Parameters\":null},{\"Name\":\"StartTelemetry\",\"Parameters\":null},{\"Name\":\"StopTelemetry\",\"Parameters\":null},{\"Name\":\"ChangeSetPointTemp\",\"Parameters\":[{\"Name\":\"SetPointTemp\",\"Type\":\"double\"}]},{\"Name\":\"DiagnosticTelemetry\",\"Parameters\":[{\"Name\":\"Active\",\"Type\":\"boolean\"}]},{\"Name\":\"ChangeDeviceState\",\"Parameters\":[{\"Name\":\"DeviceState\",\"Type\":\"string\"}]}],\"CommandHistory\":[],\"IsSimulatedDevice\":false,\"Version\":\"1.0\",\"ObjectType\":\"DeviceInfo\"}";

            // Device Info for Unit Test
            //string deviceInfo = "{\"DeviceProperties\":{\"DeviceID\":\"" + deviceID + "\",\"HubEnabledState\":true,\"CreatedTime\":\"" + createdDateTime + "\",\"DeviceState\":\"normal\",\"UpdatedTime\":null,\"Manufacturer\":\"Microsoft Corp\",\"ModelNumber\":\"Surface Pro 3\",\"SerialNumber\":\"n/a\",\"FirmwareVersion\":\"n/a\",\"Platform\":\"Windows 10 Enterprise\",\"Processor\":\"Intel Core i7-4650U CPU\",\"InstalledRAM\":\"8 GB\",\"Latitude\":" + latitude.ToString() + ",\"Longitude\":" + longitude.ToString() + "},\"Commands\":[{\"Name\":\"PingDevice\",\"Parameters\":null},{\"Name\":\"StartTelemetry\",\"Parameters\":null},{\"Name\":\"StopTelemetry\",\"Parameters\":null},{\"Name\":\"ChangeSetPointTemp\",\"Parameters\":[{\"Name\":\"SetPointTemp\",\"Type\":\"double\"}]},{\"Name\":\"DiagnosticTelemetry\",\"Parameters\":[{\"Name\":\"Active\",\"Type\":\"boolean\"}]},{\"Name\":\"ChangeDeviceState\",\"Parameters\":[{\"Name\":\"DeviceState\",\"Type\":\"string\"}]}],\"CommandHistory\":[],\"IsSimulatedDevice\":false,\"Version\":\"1.0\",\"ObjectType\":\"DeviceInfo\"}";

            // Device Info for MS Band
            string deviceInfo = "{\"DeviceProperties\":{\"DeviceID\":\"" + DEVICE_ID + "\",\"HubEnabledState\":true,\"CreatedTime\":\"" + createdDateTime + "\",\"DeviceState\":\"normal\",\"UpdatedTime\":null,\"Manufacturer\":\"Microsoft Corp\",\"ModelNumber\":\"Band - Model 1619\",\"SerialNumber\":\"007139151249\",\"FirmwareVersion\":\"10.3.3304.0 09 R\",\"Platform\":\"Windows Phone 8.1\",\"Processor\":\"ARM\",\"InstalledRAM\":\"4GB\",\"Latitude\":" + latitude.ToString() + ",\"Longitude\":" + longitude.ToString() + "},\"Commands\":[{\"Name\":\"PingDevice\",\"Parameters\":null},{\"Name\":\"StartTelemetry\",\"Parameters\":null},{\"Name\":\"StopTelemetry\",\"Parameters\":null},{\"Name\":\"ChangeSetPointTemp\",\"Parameters\":[{\"Name\":\"SetPointTemp\",\"Type\":\"double\"}]},{\"Name\":\"DiagnosticTelemetry\",\"Parameters\":[{\"Name\":\"Active\",\"Type\":\"boolean\"}]},{\"Name\":\"ChangeDeviceState\",\"Parameters\":[{\"Name\":\"DeviceState\",\"Type\":\"string\"}]}],\"CommandHistory\":[],\"IsSimulatedDevice\":false,\"Version\":\"1.0\",\"ObjectType\":\"DeviceInfo\"}";

            // Device Info for RPi
            //string deviceInfo = "{\"DeviceProperties\":{\"DeviceID\":\"" + deviceID + "\",\"HubEnabledState\":true,\"CreatedTime\":\"" + createdDateTime + "\",\"DeviceState\":\"normal\",\"UpdatedTime\":null,\"Manufacturer\":\"Nexcom\",\"ModelNumber\":\"Nise 50\",\"SerialNumber\":\"n/a\",\"FirmwareVersion\":\"10.0.10556\",\"Platform\":\"Windows\",\"Processor\":\"Intel\",\"InstalledRAM\":\"1 GB\",\"Latitude\":" + latitude.ToString() + ",\"Longitude\":" + longitude.ToString() + "},\"Commands\":[{\"Name\":\"PingDevice\",\"Parameters\":null},{\"Name\":\"StartTelemetry\",\"Parameters\":null},{\"Name\":\"StopTelemetry\",\"Parameters\":null},{\"Name\":\"ChangeSetPointTemp\",\"Parameters\":[{\"Name\":\"SetPointTemp\",\"Type\":\"double\"}]},{\"Name\":\"DiagnosticTelemetry\",\"Parameters\":[{\"Name\":\"Active\",\"Type\":\"boolean\"}]},{\"Name\":\"ChangeDeviceState\",\"Parameters\":[{\"Name\":\"DeviceState\",\"Type\":\"string\"}]}],\"CommandHistory\":[],\"IsSimulatedDevice\":false,\"Version\":\"1.0\",\"ObjectType\":\"DeviceInfo\"}";

            System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + "Updating device info message to IoTHub...\n");
            System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + "Sending Device Info: Data: [{0}]", deviceInfo);

            SendEvent(deviceInfo);

        }


        static public void SendEvent(string eventData)
        {
            string entity = Fx.Format("/devices/{0}/messages/events", DEVICE_ID);

            SenderLink senderLink = new SenderLink(session, "sender-link", entity);

            var messageValue = Encoding.UTF8.GetBytes(eventData);
            Message message = new Message()
            {
                BodySection = new Data() { Binary = messageValue }
            };

            senderLink.Send(message);
            senderLink.Close();
        }

        /// <summary>
        /// Sends Microsoft Band Telemetry in JSON format to Azure IoT Hub via AMQP
        /// </summary>
        /// <param name="bandTelemetry"></param>
        static public void SendMicrosoftBandTelemetry(MicrosoftBandTelemetry bandTelemetry)
        {
            string entity = Fx.Format("/devices/{0}/messages/events", DEVICE_ID);
            string eventData;

            SenderLink senderLink = new SenderLink(session, "sender-link", entity);

            if (String.IsNullOrEmpty(bandTelemetry.DeviceId))
                bandTelemetry.DeviceId = DEVICE_ID;

            if (serializer == null) {
                serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
            }


            using (StringWriter sw = new StringWriter())
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, bandTelemetry);
                eventData = sw.ToString();
            }
            

            var messageValue = Encoding.UTF8.GetBytes(eventData);
            Message message = new Message()
            {
                BodySection = new Data() { Binary = messageValue }
            };

            senderLink.Send(message);
            senderLink.Close();
        }

        public static async Task ReceiveCommandsAsync()
        {
            // Start Background Job
            Task backgroundWorkTask = Task.Run(() => ReceiveCommands());

            // Do more useful work

            // Wait async for BackgroundWork to complete
            // It won't block the UI thread that we're running on
            await backgroundWorkTask;

        }

        static private void ReceiveCommands()
        {
            string entity = Fx.Format("/devices/{0}/messages/deviceBound", DEVICE_ID);

            ReceiverLink receiveLink = new ReceiverLink(session, "receive-link", entity);

            Message received = receiveLink.Receive();
            if (received != null)
                receiveLink.Accept(received);
                
            receiveLink.Close();
        }

        static private bool PutCbsToken(Connection connection, string host, string shareAccessSignature, string audience)
        {
            bool result = true;
            Session session = new Session(connection);

            string cbsReplyToAddress = "cbs-reply-to";
            var cbsSender = new SenderLink(session, "cbs-sender", "$cbs");
            var cbsReceiver = new ReceiverLink(session, cbsReplyToAddress, "$cbs");

            // construct the put-token message
            var request = new Message(shareAccessSignature);
            request.Properties = new Properties();
            request.Properties.MessageId = Guid.NewGuid().ToString();
            request.Properties.ReplyTo = cbsReplyToAddress;
            request.ApplicationProperties = new ApplicationProperties();
            request.ApplicationProperties["operation"] = "put-token";
            request.ApplicationProperties["type"] = "azure-devices.net:sastoken";
            request.ApplicationProperties["name"] = audience;
            cbsSender.Send(request);

            // receive the response
            var response = cbsReceiver.Receive();
            if (response == null || response.Properties == null || response.ApplicationProperties == null)
            {
                result = false;
            }
            else
            {
                int statusCode = (int)response.ApplicationProperties["status-code"];
                string statusCodeDescription = (string)response.ApplicationProperties["status-description"];
                if (statusCode != (int)202 && statusCode != (int)200) // !Accepted && !OK
                {
                    result = false;
                }
            }

            // the sender/receiver may be kept open for refreshing tokens
            cbsSender.Close();
            cbsReceiver.Close();
            session.Close();

            return result;
        }

        private static readonly long UtcReference = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).Ticks;

        static string GetSharedAccessSignature(string keyName, string sharedAccessKey, string resource, TimeSpan tokenTimeToLive)
        {
            // http://msdn.microsoft.com/en-us/library/azure/dn170477.aspx
            // the canonical Uri scheme is http because the token is not amqp specific
            // signature is computed from joined encoded request Uri string and expiry string

#if NETMF
            // needed in .Net Micro Framework to use standard RFC4648 Base64 encoding alphabet
            System.Convert.UseRFC4648Encoding = true;
#endif
            string expiry = ((long)(DateTime.UtcNow - new DateTime(UtcReference, DateTimeKind.Utc) + tokenTimeToLive).TotalSeconds()).ToString();
            string encodedUri = HttpUtility.UrlEncode(resource);

            byte[] hmac = SHA.computeHMAC_SHA256(Convert.FromBase64String(sharedAccessKey), Encoding.UTF8.GetBytes(encodedUri + "\n" + expiry));
            string sig = Convert.ToBase64String(hmac);

            if (keyName != null)
            {
                return Fx.Format(
                "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}",
                encodedUri,
                HttpUtility.UrlEncode(sig),
                HttpUtility.UrlEncode(expiry),
                HttpUtility.UrlEncode(keyName));
            }
            else
            {
                return Fx.Format(
                    "SharedAccessSignature sr={0}&sig={1}&se={2}",
                    encodedUri,
                    HttpUtility.UrlEncode(sig),
                    HttpUtility.UrlEncode(expiry));
            }
        }
        
    }
}

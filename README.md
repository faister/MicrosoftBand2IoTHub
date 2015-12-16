# MicrosoftBand2IoTHub
This is a Microsoft Band cross-platform app that connects to the Azure IoT Hub. It is a pass-through app that bridges traffic between
a connected Microsoft Band (1 or 2) device and Iot Hub. It allows bi-directional communication with the IoT Hub. 
Leverages the Xamarin.Microsoft.Band and Microsoft.Band SDK Nuget packages. 
NOTE: Currently only Windows Phone 8.1 app is implemented. Apps for other  platforms such as Windows 8, 10, Android and iOS are coming soon.

## How to Build and Run

### Prerequisites

This app requires Azure IoT Hub. The easiest way to create an IoT Hub is by preconfiguring a remote monitoring solution in the 
Azure IoT Suite. To provision a unit of the Azure IoT Suite, please sign in using an account with Microsoft Azure subscription administrator privilege at https://www.azureiotsuite.com. Create a remote monitoring solution.

### Adding a Device

1. Log in using the account with Global Administrator rights on the remote monitoring solution dashboard. If you named your remote monitoring 
solution, "REMOTE", the URL to your dashboard will be https://remote.azurewebsites.net/.
2. Add a Device.
3. Under "Custom Device", click "Add New".
4. Define your own Device ID so that it is easier to find your device in the remote monitoring dashboard.
5. Copy the Device ID, IoT Hub Hostname, and Device Key into a blank text file, i.e., in Notepad. You will need copy and paste these credentials into the MicrosoftBand2IoTHub app settings later. Email this file to yourself so that you can copy and paste the credentials into the app as described in the steps below.

### Device List

After you have created your custom device, your device shows the status as "Pending". The reason behind this is because the device has been registered but it is not present. In order to establish presence for this device, a specific DeviceInfo message must be sent from the device. In this particular demo, it is the MicrosoftBand2IoTHub app which sends the DeviceInfo message. It retrieves the following information from the connected Microsoft Band; model number, firmware. The GPS location is retrieved from the phone.

### Deploy and Run the App on your Phone

1. Open the solution in Visual Studio 2015.
2. Rebuild the solution. This would restore any missing Nuget packages.
3. Deploy this app to the Device. In the case of a Windows Phone, you will be able to deploy directly onto your device. If you get an error that says "Unable to debug Windows Store app", try to restart the application. If problem persists and you just want to run the app, just go to the Debug menu, and Start Without Debugging (Ctrl+F5).

### Configure App Settings

1. When your app's main page appears, click the Settings button in the application bar at the bottom of the screen.
2. Copy and paste the credentials from one of the steps above.
3. Choose the duration for which you will run the telemetry ingestion onto Azure IoT Hub.
4. Click Start.
5. You will see your connected Microsoft Band, and in awhile the reading will be updated on the results area.

It’s important to understand that subscribing to sensor data effects the battery life of the Band. The use of each sensor requires a power draw (some more than others). This is the reason why you have to select a duration for the telemetry ingestion.
On Windows and iOS, constant connectivity with the Microsoft Band device is required to maintain a subscription. If the Band loses connectivity with the phone, the subscription is stopped and it’s not automatically enabled upon reconnection.

The following sensors are only available in Microsoft Band 2:

1. Barometer data - Provides the current raw air pressure in hPa (hectopascals) and raw temperature in degrees Celsius.
2. Galvanic Skin Response - Provides the current skin resistance of the wearer in kohms.
3. Ambient Light - Provides the current light intensity (illuminance) in lux (Lumes per sq. meter).
4. Altimeter - Provides current elevation data like total gain/loss, steps ascended/descended, flights ascended/descended, and elevation rate.

### Microsoft Band biometric telemetry 

Band telemetry are defined in a MicrosoftBandTelemetry class. If the respective properties are present and read from the device, they are serialized into a JSON string. Essentially the JSON string is the event data which is sent to the IoT Hub. You may perform your own complex event processing (CEP) in Azure IoT Suite by using either Azure Stream Analytics, developing your own EventProcessorHost and hosting within a WebJob, or using any other stream processing methods.

    public class MicrosoftBandTelemetry
    {
        public string DeviceId { get; set; }
        public string Accelerometer { get; set; }
        public string Altimeter { get; set; }
        public string AmbientLight { get; set; }
        public string Barometer { get; set; }
        public string Calories { get; set; }
        public string Contact { get; set; }
        public string Distance { get; set; }
        public string Gyroscope { get; set; }
        public string Gsr { get; set; }
        public string HeartRate { get; set; }
        public string Pedometer { get; set; }
        public string RRInterval { get; set; }
        public string SkinTemperature { get; set; }
        public string UltravioletLight { get; set; }
        public string AirPressure { get; internal set; }
        public string GsrResistance { get; internal set; }
        public string Brightness { get; internal set; }
        public string Temperature { get; internal set; }
        public string FlightsAscended { get; internal set; }
        public string FlightsDescended { get; internal set; }
        public string AltimeterRate { get; internal set; }
        public string SteppingGain { get; internal set; }
        public string SteppingLoss { get; internal set; }
        public string StepsAscended { get; internal set; }
        public string StepsDescended { get; internal set; }
        public string TotalGain { get; internal set; }
        public string TotalLoss { get; internal set; }
        public string Humidity { get; internal set; }
    }

The event data in JSON looks like the following:
Data:[{"DeviceId":"SydneyBand","Calories":"436436","Distance":"78008489","HeartRate":"69","SkinTemperature":"34.49","Temperature":"34.49","Humidity":"69"}]

You may have noticed that there are two additional data points in the JSON string which are weird. I have repeated the values for heart rate and skin temperature as "Humidity" and "Temperature" respectively. The reason for this is because I just want these telemetry data to be visualized on the Azure IoT Suite remote monitoring web dashboard as is without any modification. 

# MicrosoftBand2IoTHub
This is a Microsoft Band cross-platform app that connects to the Azure IoT Hub. It is a pass-through app that bridges traffic between
a connected Microsoft Band (1 or 2) device and Iot Hub. It allows bi-directional communication with the IoT Hub. 
Leverages the Xamarin.Microsoft.Band and Microsoft.Band SDK Nuget packages. 
NOTE: Currently only Windows Phone 8.1 app is implemented. Apps for other  platforms such as Windows 8, 10, Android and iOS are coming soon.

## How to Build and Run

### Prerequisites

This app requires an Azure IoT Hub. The easiest way to create an IoT Hub is by preconfiguring a remote monitoring solution in the 
Azure IoT Suite. To provision a unit of the Azure IoT Suite, please sign in using an account with Microsoft Azure subscription administrator
privilege at https://www.azureiotsuite.com. Create a remote monitoring solution.

### Adding a Device

1. Log in using the account with Global Administrator rights on the remote monitoring solution dashboard. If you named your remote monitoring 
solution, "REMOTE", the URL to your dashboard will be https://remote.azurewebsites.net/.
2. Add a Device.
3. Under "Custom Device", click "Add New".
4. Define your own Device ID so that it is easier to find your device in the remote monitoring dashboard.
5. Copy the Device ID, IoT Hub Hostname, and Device Key into a blank text file, i.e., in Notepad. You will need copy and paste these credentials into the MicrosoftBand2IoTHub app settings later.

### Device List

After you have created your custom device, your device shows the status as "Pending". The reason behind this is because the device has been registered but it is not present. In order to establish presence for this device, a specific DeviceInfo message must be sent from the device. In this particular demo, it is the MicrosoftBand2IoTHub app which sends the DeviceInfo message. It retrieves the following information from the connected Microsoft Band; model number, firmware. The GPS location is retrieved from the phone.

### Deploy and Run the App on your Phone

1. Open the solution in Visual Studio 2015.
2. Rebuild the solution. This would restore any missing Nuget packages.
3. Deploy this app to the Device. In the case of a Windows Phone, you will be able to deploy directly onto your device.

### Configure App Settings

1. When your app's main page appears, click the Settings button in the application bar at the bottom of the screen.
2. Copy and paste the credentials from one of the steps above.
3. Choose the duration for which you will run the telemetry ingestion onto Azure IoT Hub.
4. Click Start.
5. You will see your connected Microsoft Band, and in awhile the reading will be updated on the results area.

It’s important to understand that subscribing to sensor data effects the battery life of the Band. The use of each sensor requires a power draw (some more than others). This is the reason why you have to select a duration for the telemetry ingestion.
On Windows and iOS, constant connectivity with the Microsoft Band device is required to maintain a subscription. If the Band loses connectivity with the phone, the subscription is stopped and it’s not automatically enabled upon reconnection.

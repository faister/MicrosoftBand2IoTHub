# MicrosoftBand2IoTHub
This is a Microsoft Band cross-platform app that connects to the Azure IoT Hub. It is a pass-through app that bridges traffic between
a connected Microsoft Band (1 or 2) device and Iot Hub. It allows bi-directional communication with the IoT Hub. 
Leverages the Xamarin.Microsoft.Band and Microsoft.Band SDK Nuget packages. 

How to Buil and Run

Prerequisites

This app requires an Azure IoT Hub. The easiest way to create an IoT Hub is by preconfiguring a remote monitoring solution in the 
Azure IoT Suite. To provision a unit of the Azure IoT Suite, please sign in using an account with Microsoft Azure subscription administrator
privilege at https://www.azureiotsuite.com. Create a remote monitoring solution.

Adding a Device

1. Log in using the account with Global Administrator rights on the remote monitoring solution dashboard. If you named your remote monitoring 
solution, "REMOTE", the URL to your dashboard will be https://remote.azurewebsites.net/.
2. Add a Device.
3. Under "Custom Device", click "Add New".
4. Define your own Device ID so that it is easier to find your device in the remote monitoring dashboard.
5. Copy the Device ID, IoT Hub Hostname, and Device Key. You will need copy and paste these credentials into the MicrosoftBand2IoTHub app settings later.



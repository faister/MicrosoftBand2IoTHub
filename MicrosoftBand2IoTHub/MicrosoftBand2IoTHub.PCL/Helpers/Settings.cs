// Helpers/Settings.cs
using Plugin.Settings;
using Plugin.Settings.Abstractions;

namespace MicrosoftBand2IoTHub.PCL.Helpers
{
  /// <summary>
  /// This is the Settings static class that can be used in your Core solution or in any
  /// of your client applications. All settings are laid out the same exact way with getters
  /// and setters. 
  /// </summary>
  public static class Settings
  {
        private static ISettings AppSettings
        {
            get
            {
                return CrossSettings.Current;
            }
        }

        #region Setting Constants

        // The key names of our settings
        private const string DeviceIDSettingKeyName = "DeviceIDSetting";
        private const string IoTHubHostNameSettingKeyName = "IoTHubSetting";
        private const string DeviceKeySettingKeyName = "DeviceKeySetting";

        // The default value of our settings
        private static readonly string DeviceIDSettingDefault = "SydneyBand";
        private static readonly string IoTHubHostNameSettingDefault = "sydneyremote.azure-devices.net";
        private static readonly string DeviceKeySettingDefault = "u9rF4kfsXktrI9QHvzlmqA==";

        #endregion

        /// <summary>
        /// Property to get and set a DeviceID 
        /// </summary>
        public static string DeviceID
        {
            get
            {
                return AppSettings.GetValueOrDefault<string>(DeviceIDSettingKeyName, DeviceIDSettingDefault);
            }
            set
            {
                AppSettings.AddOrUpdateValue<string>(DeviceIDSettingKeyName, value);
            }
        }

        /// <summary>
        /// Property to get and set a IoT Hub Hostname
        /// </summary>
        public static string IoTHubHostName
        {
            get
            {
                return AppSettings.GetValueOrDefault<string>(IoTHubHostNameSettingKeyName, IoTHubHostNameSettingDefault);
            }
            set
            {
                AppSettings.AddOrUpdateValue<string>(IoTHubHostNameSettingKeyName, value);
            }
        }


        /// <summary>
        /// Property to get and set a Device Key
        /// </summary>
        public static string DeviceKey
        {
            get
            {
                return AppSettings.GetValueOrDefault<string>(DeviceKeySettingKeyName, DeviceKeySettingDefault);
            }
            set
            {
                AppSettings.AddOrUpdateValue<string>(DeviceKeySettingKeyName, value);
            }
        }



    }
}
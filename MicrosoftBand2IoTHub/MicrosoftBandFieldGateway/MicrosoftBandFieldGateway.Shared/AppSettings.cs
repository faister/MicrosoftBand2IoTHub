using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Diagnostics;

namespace MicrosoftBandFieldGateway
{
    public class AppSettings
    {
        // Our settings
        ApplicationDataContainer settings;

        // The key names of our settings
        const string DeviceIDSettingKeyName = "DeviceIDSetting";
        const string IoTHubHostNameSettingKeyName = "IoTHubSetting";
        const string DeviceKeySettingKeyName = "DeviceKeySetting";

        // The default value of our settings
        const string DeviceIDSettingDefault = "MicrosoftBand";
        const string IoTHubHostNameSettingDefault = "sydneyremote.azure-devices.net";
        const string DeviceKeySettingDefault = "SAqvmtrMLbPQoKdxdg4WyA==";
        //const string DeviceIDSettingDefault = "ClayBand";
        //const string IoTHubHostNameSettingDefault = "ClayRemote.azure-devices.net";
        //const string DeviceKeySettingDefault = "LBbvC0/wvDP0c2hoyIancYQxwPCaGXeVQPfA30ilDUM=";

        /// <summary>
        /// Constructor that gets the application settings.
        /// </summary>
        public AppSettings()
        {
            // Get the settings for this application.
            settings = Windows.Storage.ApplicationData.Current.LocalSettings;
        }

        /// <summary>
        /// Update a setting value for our application. If the setting does not
        /// exist, then add the setting.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool AddOrUpdateValue(string Key, Object value)
        {
            bool valueChanged = false;

            // If the key exists
            if (settings.Values.ContainsKey(Key))
            {
                // If the value has changed
                if (settings.Values[Key] != value)
                {
                    // Store the new value
                    settings.Values[Key] = value;
                    valueChanged = true;
                }
            }
            // Otherwise create the key.
            else
            {
                settings.Values[Key] = value;
                valueChanged = true;
            }
            return valueChanged;
        }

        /// <summary>
        /// Get the current value of the setting, or if it is not found, set the 
        /// setting to the default setting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T GetValueOrDefault<T>(string Key, T defaultValue)
        {
            T value = defaultValue;

            // If the key exists, retrieve the value.
            if (settings.Values.ContainsKey(Key))
            {
                value = (T)settings.Values[Key];
            }
            // Otherwise, use the default value.
            else
            {
                value = defaultValue;
            }
            return value;
        }

        /// <summary>
        /// Save the settings.
        /// </summary>
        public void Save()
        {
            //settings.Save();
        }

        /// <summary>
        /// Property to get and set a DeviceID 
        /// </summary>
        public string DeviceID
        {
            get
            {
                return GetValueOrDefault<string>(DeviceIDSettingKeyName, DeviceIDSettingDefault);
            }
            set
            {
                if (AddOrUpdateValue(DeviceIDSettingKeyName, value))
                {
                    Save();
                }
            }
        }

        /// <summary>
        /// Property to get and set a IoT Hub Hostname
        /// </summary>
        public string IoTHubHostName
        {
            get
            {
                return GetValueOrDefault<string>(IoTHubHostNameSettingKeyName, IoTHubHostNameSettingDefault);
            }
            set
            {
                if (AddOrUpdateValue(IoTHubHostNameSettingKeyName, value))
                {
                    Save();
                }
            }
        }


        /// <summary>
        /// Property to get and set a Device Key
        /// </summary>
        public string DeviceKey
        {
            get
            {
                return GetValueOrDefault<string>(DeviceKeySettingKeyName, DeviceKeySettingDefault);
            }
            set
            {
                if (AddOrUpdateValue(DeviceKeySettingKeyName, value))
                {
                    Save();
                }
            }
        }

    }
}

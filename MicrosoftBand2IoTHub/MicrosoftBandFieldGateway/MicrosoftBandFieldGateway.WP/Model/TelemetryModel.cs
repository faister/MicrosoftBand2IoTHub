using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace MicrosoftBandFieldGateway.Model
{
    public class TelemetryModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _statusMessage;
        public string StatusMessage
        {
            get
            {
                return _statusMessage;
            }
            set
            {
                _statusMessage = value;
                RaisePropertyChanged("StatusMessage");
            }

        }

        private string _buttonContent;
        public string ButtonContent
        {
            get
            {
                return _buttonContent;
            }
            set
            {
                _buttonContent = value;
                RaisePropertyChanged("ButtonContent");
            }

        }

        private string _heartRate;
        public string HeartRate
        {
            get
            {
                return _heartRate;
            }
            set
            {
                _heartRate = value;
                RaisePropertyChanged("HeartRate");
            }

        }

        private string _SkinTemperature;
        public string SkinTemperature
        {
            get
            {
                return _SkinTemperature;
            }
            set
            {
                _SkinTemperature = value;
                RaisePropertyChanged("SkinTemperature");
            }

        }

        private string _Pedometer;
        public string Pedometer
        {
            get
            {
                return _Pedometer;
            }
            set
            {
                _Pedometer = value;
                RaisePropertyChanged("Pedometer");
            }

        }


        private string _Distance;
        public string Distance
        {
            get
            {
                return _Distance;
            }
            set
            {
                _Distance = value;
                RaisePropertyChanged("Distance");
            }

        }


        private string _Calories;
        public string Calories
        {
            get
            {
                return _Calories;
            }
            set
            {
                _Calories = value;
                RaisePropertyChanged("Calories");
            }

        }


        private string _AirPressure;
        public string AirPressure
        {
            get
            {
                return _AirPressure;
            }
            set
            {
                _AirPressure = value;
                RaisePropertyChanged("AirPressure");
            }

        }


        private string _GsrResistance;
        public string GsrResistance
        {
            get
            {
                return _GsrResistance;
            }
            set
            {
                _GsrResistance = value;
                RaisePropertyChanged("GsrResistance");
            }

        }

        private string _Brightness;
        public string Brightness
        {
            get
            {
                return _Brightness;
            }
            set
            {
                _Brightness = value;
                RaisePropertyChanged("Brightness");
            }

        }

        private string _AltimeterRate;
        public string AltimeterRate
        {
            get
            {
                return _AltimeterRate;
            }
            set
            {
                _AltimeterRate = value;
                RaisePropertyChanged("AltimeterRate");
            }

        }


        private void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // Create a copy of an TelemetryReading to save.
        // If your object is databound, this copy is not databound.
        public TelemetryModel GetCopy()
        {
            TelemetryModel copy = (TelemetryModel)this.MemberwiseClone();
            return copy;
        }


    }
}

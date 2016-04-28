using System;
using System.Collections.Generic;
using System.Text;

namespace MicrosoftBandFieldGateway
{
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
        public int HeartRate { get; set; }
        public string Pedometer { get; set; }
        public string RRInterval { get; set; }
        public double SkinTemperature { get; set; }
        public string UltravioletLight { get; set; }
        public string AirPressure { get; internal set; }
        public string GsrResistance { get; internal set; }
        public int Brightness { get; internal set; }
        public double Temperature { get; internal set; }
        public string FlightsAscended { get; internal set; }
        public string FlightsDescended { get; internal set; }
        public string AltimeterRate { get; internal set; }
        public string SteppingGain { get; internal set; }
        public string SteppingLoss { get; internal set; }
        public string StepsAscended { get; internal set; }
        public string StepsDescended { get; internal set; }
        public string TotalGain { get; internal set; }
        public string TotalLoss { get; internal set; }
        public double Humidity { get; internal set; }
    }
}

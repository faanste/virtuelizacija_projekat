using System;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class SensorSample
    {
        [DataMember]
        public double Volume { get; set; }

        [DataMember]
        public double T_DHT { get; set; }

        [DataMember]
        public double T_BMP { get; set; }

        [DataMember]
        public double Pressure { get; set; }

        [DataMember]
        public DateTime DateTime { get; set; }
    }
}
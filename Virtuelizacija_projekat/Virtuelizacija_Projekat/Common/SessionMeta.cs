using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class SessionMeta
    {
        [DataMember]
        public string SessionId { get; set; }

        [DataMember]
        public string Volume { get; set; }

        [DataMember]
        public string T_DHT { get; set; }

        [DataMember]
        public string T_BMP { get; set; }

        [DataMember]
        public string Pressure { get; set; }

        [DataMember]
        public string DateTime { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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

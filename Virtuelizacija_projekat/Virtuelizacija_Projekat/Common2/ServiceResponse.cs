using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;


namespace Common
{
    [DataContract]
    public class ServiceResponse
    {
        [DataMember]
        public bool Ack { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public TransferStatus Status { get; set; }
    }
}

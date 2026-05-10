using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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

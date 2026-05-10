using System.Runtime.Serialization;

namespace Common.Faults
{
    [DataContract]
    public class DataFormatFault
    {
        [DataMember]
        public string Message { get; set; }
    }
}
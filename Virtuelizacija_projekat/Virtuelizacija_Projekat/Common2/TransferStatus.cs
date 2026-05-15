using System.Runtime.Serialization;


namespace Common
{
    public enum TransferStatus
    {
        [EnumMember]
        IN_PROGRESS,

        [EnumMember]
        COMPLETED
    }
}

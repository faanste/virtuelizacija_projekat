using Common;
using System;

namespace Server
{
    public delegate void TransferStartedHandler(object sender, TransferStartedEventArgs e);
    public delegate void SampleReceivedHandler(object sender, SampleReceivedEventArgs e);
    public delegate void TransferCompletedHandler(object sender, TransferCompletedEventArgs e);
    public delegate void WarningRaisedHandler(object sender, WarningRaisedEventArgs e);

    public class TransferStartedEventArgs : EventArgs
    {
        public string SessionId { get; set; }
        public DateTime StartTime { get; set; }
    }

    public class SampleReceivedEventArgs : EventArgs
    {
        public SensorSample Sample { get; set; }
        public int SampleNumber { get; set; }
    }

    public class TransferCompletedEventArgs : EventArgs
    {
        public int TotalSamples { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class WarningRaisedEventArgs : EventArgs
    {
        public string WarningType { get; set; }
        public string Message { get; set; }
        public string Direction { get; set; }
        public double CurrentValue { get; set; }
        public double ExpectedValue { get; set; }
        public double Threshold { get; set; }
        public DateTime Time { get; set; }
    }
}
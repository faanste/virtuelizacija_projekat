using Common;
using Common.Faults;
using System;
using System.Configuration;
using System.Globalization;
using System.ServiceModel;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SensorService : ISensorService
    {
        public event TransferStartedHandler OnTransferStarted;
        public event SampleReceivedHandler OnSampleReceived;
        public event TransferCompletedHandler OnTransferCompleted;
        public event WarningRaisedHandler OnWarningRaised;

        public event VolumeSpikeHandler OnVolumeSpike;
        public event OutOfBandWarningHandler OnOutOfBandWarning;

        public event TemperatureSpikeDHTHandler OnTemperatureSpikeDHT;
        public event TemperatureSpikeBMPHandler OnTemperatureSpikeBMP;

        private bool sessionStarted = false;
        private MeasurementFileWriter fileWriter;
        private readonly string serverDataPath;

        private int receivedSamples = 0;
        private DateTime transferStartTime;

        private SensorSample previousSample = null;
        private double volumeSum = 0;
        private int volumeMeanCount = 0;

        private readonly double vThreshold;
        private readonly double tDhtThreshold;
        private readonly double tBmpThreshold;
        private readonly double meanDeviationPercent;

        public SensorService()
        {
            serverDataPath = ConfigurationManager.AppSettings["serverDataPath"];

            if (string.IsNullOrWhiteSpace(serverDataPath))
            {
                serverDataPath = "data";
            }

            vThreshold = ReadDoubleSetting("V_threshold", 100);
            tDhtThreshold = ReadDoubleSetting("T_dht_threshold", 2);
            tBmpThreshold = ReadDoubleSetting("T_bmp_threshold", 2);
            meanDeviationPercent = ReadDoubleSetting("MeanDeviationPercent", 0.25);

            if (meanDeviationPercent > 1)
            {
                meanDeviationPercent = meanDeviationPercent / 100;
            }

            SubscribeToEvents();
        }

        public ServiceResponse StartSession(SessionMeta meta)
        {
            ValidateMeta(meta);

            if (sessionStarted)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Sesija je vec pokrenuta." },
                    new FaultReason("Sesija je vec pokrenuta."));
            }

            fileWriter = new MeasurementFileWriter(serverDataPath);

            sessionStarted = true;
            receivedSamples = 0;
            previousSample = null;
            volumeSum = 0;
            volumeMeanCount = 0;
            transferStartTime = DateTime.Now;

            RaiseTransferStarted(new TransferStartedEventArgs
            {
                SessionId = meta.SessionId,
                StartTime = transferStartTime
            });

            Console.WriteLine("Meta podaci:");
            Console.WriteLine("Volume: " + meta.Volume);
            Console.WriteLine("T_DHT: " + meta.T_DHT);
            Console.WriteLine("T_BMP: " + meta.T_BMP);
            Console.WriteLine("Pressure: " + meta.Pressure);
            Console.WriteLine("DateTime: " + meta.DateTime);

            return new ServiceResponse
            {
                Ack = true,
                Message = "StartSession uspesno izvrsen.",
                Status = TransferStatus.IN_PROGRESS
            };
        }

        public ServiceResponse PushSample(SensorSample sample)
        {
            if (!sessionStarted)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Ne moze se poslati sample jer sesija nije pokrenuta." },
                    new FaultReason("Sesija nije pokrenuta."));
            }

            try
            {
                ValidateSample(sample);
            }
            catch (FaultException<ValidationFault> ex)
            {
                WriteRejectSafe(sample, ex.Detail.Message);
                throw;
            }
            catch (FaultException<DataFormatFault> ex)
            {
                WriteRejectSafe(sample, ex.Detail.Message);
                throw;
            }

            fileWriter.WriteMeasurement(sample);

            receivedSamples++;

            RaiseSampleReceived(new SampleReceivedEventArgs
            {
                Sample = sample,
                SampleNumber = receivedSamples
            });

            CheckVolumeSpike(sample);
            CheckOutOfBandWarning(sample);
            CheckTemperatureSpikes(sample);

            UpdateRunningMean(sample);
            previousSample = sample;

            return new ServiceResponse
            {
                Ack = true,
                Message = "Sample je uspesno primljen i upisan u fajl.",
                Status = TransferStatus.IN_PROGRESS
            };
        }

        public ServiceResponse EndSession()
        {
            if (!sessionStarted)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Ne moze se zavrsiti sesija jer nije ni pokrenuta." },
                    new FaultReason("Sesija nije pokrenuta."));
            }

            sessionStarted = false;

            if (fileWriter != null)
            {
                fileWriter.Dispose();
                fileWriter = null;
            }

            DateTime transferEndTime = DateTime.Now;
            TimeSpan duration = transferEndTime - transferStartTime;

            RaiseTransferCompleted(new TransferCompletedEventArgs
            {
                TotalSamples = receivedSamples,
                EndTime = transferEndTime,
                Duration = duration
            });

            return new ServiceResponse
            {
                Ack = true,
                Message = "EndSession uspesno izvrsen. Fajlovi su zatvoreni.",
                Status = TransferStatus.COMPLETED
            };
        }

        private void SubscribeToEvents()
        {
            OnTransferStarted += HandleTransferStarted;
            OnSampleReceived += HandleSampleReceived;
            OnTransferCompleted += HandleTransferCompleted;
            OnWarningRaised += HandleWarningRaised;

            OnVolumeSpike += HandleVolumeSpike;
            OnOutOfBandWarning += HandleOutOfBandWarning;
            OnTemperatureSpikeDHT += HandleTemperatureSpikeDHT;
            OnTemperatureSpikeBMP += HandleTemperatureSpikeBMP;
        }

        private void HandleTransferStarted(object sender, TransferStartedEventArgs e)
        {
            Console.WriteLine("======================================");
            Console.WriteLine("[EVENT] OnTransferStarted");
            Console.WriteLine("PRENOS U TOKU...");
            Console.WriteLine("Sesija: " + e.SessionId);
            Console.WriteLine("Vreme pocetka: " + e.StartTime);
            Console.WriteLine("======================================");
        }

        private void HandleSampleReceived(object sender, SampleReceivedEventArgs e)
        {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("[EVENT] OnSampleReceived");
            Console.WriteLine("PRENOS U TOKU...");
            Console.WriteLine("Primljen sample broj: " + e.SampleNumber);
            Console.WriteLine("Volume: " + e.Sample.Volume);
            Console.WriteLine("T_DHT: " + e.Sample.T_DHT);
            Console.WriteLine("T_BMP: " + e.Sample.T_BMP);
            Console.WriteLine("Pressure: " + e.Sample.Pressure);
            Console.WriteLine("DateTime: " + e.Sample.DateTime);
            Console.WriteLine("--------------------------------------");
        }

        private void HandleTransferCompleted(object sender, TransferCompletedEventArgs e)
        {
            Console.WriteLine("======================================");
            Console.WriteLine("[EVENT] OnTransferCompleted");
            Console.WriteLine("ZAVRSEN PRENOS");
            Console.WriteLine("Ukupno primljenih uzoraka: " + e.TotalSamples);
            Console.WriteLine("Vreme zavrsetka: " + e.EndTime);
            Console.WriteLine("Trajanje prenosa: " + e.Duration.TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture) + " sekundi");
            Console.WriteLine("======================================");
        }

        private void HandleWarningRaised(object sender, WarningRaisedEventArgs e)
        {
            Console.WriteLine("======================================");
            Console.WriteLine("[EVENT] OnWarningRaised");
            Console.WriteLine("Tip upozorenja: " + e.WarningType);
            Console.WriteLine("Poruka: " + e.Message);
            Console.WriteLine("Smer: " + e.Direction);
            Console.WriteLine("Trenutna vrednost: " + e.CurrentValue.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Ocekivana vrednost: " + e.ExpectedValue.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Prag: " + e.Threshold.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Vreme: " + e.Time);
            Console.WriteLine("=====================================");
        }


        private void HandleVolumeSpike(object sender, VolumeSpikeEventArgs e)
        {
            Console.WriteLine("=======================================");
            Console.WriteLine("[EVENT] VolumeSpike");
            Console.WriteLine("Vreme: " + e.Time);
            Console.WriteLine("Prethodni Volume: " + e.PreviousVolume.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Trenutni Volume: " + e.CurrentVolume.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Delta: " + e.DeltaV.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Prag: " + e.Threshold.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Smjer: " + e.Direction);
            Console.WriteLine("=======================================");
        }

        private void HandleOutOfBandWarning(object sender, OutOfBandWarningEventArgs e)
        {
            Console.WriteLine("=======================================");
            Console.WriteLine("[EVENT] OutOfBandWarning");
            Console.WriteLine("Vreme: " + e.Time);
            Console.WriteLine("Trenutni Volume: " + e.CurrentVolume.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Tekuci prosjek: " + e.Mean.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Granica: " + e.Limit.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Smjer: " + e.Direction);
            Console.WriteLine("=======================================");
        }

        private void HandleTemperatureSpikeDHT(object sender, TemperatureSpikeEventArgs e)
        {
            Console.WriteLine("=======================================");
            Console.WriteLine("[EVENT] TemperatureSpikeDHT");
            Console.WriteLine("Vreme: " + e.Time);
            Console.WriteLine("Prethodna T_DHT: " + e.PreviousValue.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Trenutna T_DHT: " + e.CurrentValue.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Delta: " + e.Delta.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Prag: " + e.Threshold.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Smjer: " + e.Direction);
            Console.WriteLine("========================================");
        }

        private void HandleTemperatureSpikeBMP(object sender, TemperatureSpikeEventArgs e)
        {
            Console.WriteLine("=====================================");
            Console.WriteLine("[EVENT] TemperatureSpikeBMP");
            Console.WriteLine("Vreme: " + e.Time);
            Console.WriteLine("Prethodna T_BMP: " + e.PreviousValue.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Trenutna T_BMP: " + e.CurrentValue.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Delta: " + e.Delta.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Prag: " + e.Threshold.ToString("0.###", CultureInfo.InvariantCulture));
            Console.WriteLine("Smjer: " + e.Direction);
            Console.WriteLine("=====================================");
        }


        private void CheckVolumeSpike(SensorSample sample)
        {
            if (previousSample == null)
                return;

            double deltaV = sample.Volume - previousSample.Volume;

            if (Math.Abs(deltaV) > vThreshold)
            {
                if (OnVolumeSpike != null)
                {
                    OnVolumeSpike(this, new VolumeSpikeEventArgs
                    {
                        Time = sample.DateTime,
                        DeltaV = deltaV,
                        CurrentVolume = sample.Volume,
                        PreviousVolume = previousSample.Volume,
                        Threshold = vThreshold,
                        Direction = deltaV > 0 ? "iznad ocekivanog" : "ispod ocekivanog"
                    });
                }

                /*
                RaiseWarningRaised(new WarningRaisedEventArgs
                {
                    WarningType = "VolumeSpike",
                    Message = "Detektovan je nagli skok buke.",
                    Direction = deltaV > 0 ? "iznad ocekivanog" : "ispod ocekivanog",
                    CurrentValue = deltaV,
                    ExpectedValue = 0,
                    Threshold = vThreshold,
                    Time = sample.DateTime
                });
                */
            }
        }


        private void CheckOutOfBandWarning(SensorSample sample)
        {
            if (volumeMeanCount == 0)
                return;

            double mean = volumeSum / volumeMeanCount;
            double lowerLimit = mean * (1 - meanDeviationPercent);
            double upperLimit = mean * (1 + meanDeviationPercent);

            if (sample.Volume < lowerLimit)
            {
                if (OnOutOfBandWarning != null)
                {
                    OnOutOfBandWarning(this, new OutOfBandWarningEventArgs
                    {
                        Time = sample.DateTime,
                        CurrentVolume = sample.Volume,
                        Mean = mean,
                        Limit = lowerLimit,
                        Direction = "ispod ocekivane vrednosti"
                    });
                }

                RaiseWarningRaised(new WarningRaisedEventArgs
                {
                    WarningType = "OutOfBandWarning",
                    Message = "Volume je ispod dozvoljenog odstupanja od tekuceg proseka.",
                    Direction = "ispod ocekivane vrednosti",
                    CurrentValue = sample.Volume,
                    ExpectedValue = mean,
                    Threshold = lowerLimit,
                    Time = sample.DateTime
                });
            }
            else if (sample.Volume > upperLimit)
            {
                if (OnOutOfBandWarning != null)
                {
                    OnOutOfBandWarning(this, new OutOfBandWarningEventArgs
                    {
                        Time = sample.DateTime,
                        CurrentVolume = sample.Volume,
                        Mean = mean,
                        Limit = upperLimit,
                        Direction = "iznad ocekivane vrednosti"
                    });
                }

                RaiseWarningRaised(new WarningRaisedEventArgs
                {
                    WarningType = "OutOfBandWarning",
                    Message = "Volume je iznad dozvoljenog odstupanja od tekuceg proseka.",
                    Direction = "iznad ocekivane vrednosti",
                    CurrentValue = sample.Volume,
                    ExpectedValue = mean,
                    Threshold = upperLimit,
                    Time = sample.DateTime
                });
            }
        }


        private void CheckTemperatureSpikes(SensorSample sample)
        {
            if (previousSample == null)
                return;

            double deltaDht = sample.T_DHT - previousSample.T_DHT;

            if (Math.Abs(deltaDht) > tDhtThreshold)
            {
                if (OnTemperatureSpikeDHT != null)
                {
                    OnTemperatureSpikeDHT(this, new TemperatureSpikeEventArgs
                    {
                        Time = sample.DateTime,
                        Delta = deltaDht,
                        CurrentValue = sample.T_DHT,
                        PreviousValue = previousSample.T_DHT,
                        Threshold = tDhtThreshold,
                        Direction = deltaDht > 0 ? "iznad ocekivanog" : "ispod ocekivanog"
                    });
                }
                /*
                RaiseWarningRaised(new WarningRaisedEventArgs
                {
                    WarningType = "TemperatureSpikeDHT",
                    Message = "Detektovan je nagli skok temperature na DHT senzoru.",
                    Direction = deltaDht > 0 ? "iznad ocekivanog" : "ispod ocekivanog",
                    CurrentValue = deltaDht,
                    ExpectedValue = 0,
                    Threshold = tDhtThreshold,
                    Time = sample.DateTime
                });
                */
            }

            double deltaBmp = sample.T_BMP - previousSample.T_BMP;

            if (Math.Abs(deltaBmp) > tBmpThreshold)
            {
                if (OnTemperatureSpikeBMP != null)
                {
                    OnTemperatureSpikeBMP(this, new TemperatureSpikeEventArgs
                    {
                        Time = sample.DateTime,
                        Delta = deltaBmp,
                        CurrentValue = sample.T_BMP,
                        PreviousValue = previousSample.T_BMP,
                        Threshold = tBmpThreshold,
                        Direction = deltaBmp > 0 ? "iznad ocekivanog" : "ispod ocekivanog"
                    });
                }
                /*
                RaiseWarningRaised(new WarningRaisedEventArgs
                {
                    WarningType = "TemperatureSpikeBMP",
                    Message = "Detektovan je nagli skok temperature na BMP senzoru.",
                    Direction = deltaBmp > 0 ? "iznad ocekivanog" : "ispod ocekivanog",
                    CurrentValue = deltaBmp,
                    ExpectedValue = 0,
                    Threshold = tBmpThreshold,
                    Time = sample.DateTime
                });
                */
            }
        }

        private void UpdateRunningMean(SensorSample sample)
        {
            volumeSum += sample.Volume;
            volumeMeanCount++;
        }


        private void RaiseTransferStarted(TransferStartedEventArgs e)
        {
            if (OnTransferStarted != null)
            {
                OnTransferStarted(this, e);
            }
        }

        private void RaiseSampleReceived(SampleReceivedEventArgs e)
        {
            if (OnSampleReceived != null)
            {
                OnSampleReceived(this, e);
            }
        }

        private void RaiseTransferCompleted(TransferCompletedEventArgs e)
        {
            if (OnTransferCompleted != null)
            {
                OnTransferCompleted(this, e);
            }
        }

        private void RaiseWarningRaised(WarningRaisedEventArgs e)
        {
            if (OnWarningRaised != null)
            {
                OnWarningRaised(this, e);
            }
        }

        private double ReadDoubleSetting(string key, double defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];

            double parsedValue;

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue))
            {
                return parsedValue;
            }

            return defaultValue;
        }

        private void WriteRejectSafe(SensorSample sample, string reason)
        {
            try
            {
                if (fileWriter != null)
                {
                    fileWriter.WriteReject(sample, reason);
                    Console.WriteLine("Odbacen sample upisan u rejects.csv. Razlog: " + reason);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greska pri upisu u rejects.csv: " + ex.Message);
            }
        }

        private void ValidateMeta(SessionMeta meta)
        {
            if (meta == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault { Message = "Meta podaci nisu prosledjeni." },
                    new FaultReason("Meta podaci nisu prosledjeni."));
            }

            if (string.IsNullOrWhiteSpace(meta.Volume) ||
                string.IsNullOrWhiteSpace(meta.T_DHT) ||
                string.IsNullOrWhiteSpace(meta.T_BMP) ||
                string.IsNullOrWhiteSpace(meta.Pressure) ||
                string.IsNullOrWhiteSpace(meta.DateTime))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Meta podaci moraju imati sva obavezna polja." },
                    new FaultReason("Nedostaju obavezna meta polja."));
            }
        }

        private void ValidateSample(SensorSample sample)
        {
            if (sample == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault { Message = "Sample nije prosledjen." },
                    new FaultReason("Sample nije prosledjen."));
            }

            if (double.IsNaN(sample.Volume) || double.IsInfinity(sample.Volume))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Volume nije validan broj." },
                    new FaultReason("Nevalidan Volume."));
            }

            if (sample.Volume < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Volume ne sme biti negativan." },
                    new FaultReason("Nevalidan Volume."));
            }

            if (double.IsNaN(sample.T_DHT) || double.IsInfinity(sample.T_DHT))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "T_DHT nije validan broj." },
                    new FaultReason("Nevalidan T_DHT."));
            }

            if (double.IsNaN(sample.T_BMP) || double.IsInfinity(sample.T_BMP))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "T_BMP nije validan broj." },
                    new FaultReason("Nevalidan T_BMP."));
            }

            if (double.IsNaN(sample.Pressure) || double.IsInfinity(sample.Pressure))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Pressure nije validan broj." },
                    new FaultReason("Nevalidan Pressure."));
            }

            if (sample.Pressure <= 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Pressure mora biti veci od 0." },
                    new FaultReason("Nevalidan Pressure."));
            }

            if (sample.DateTime == default(DateTime))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "DateTime nije validan." },
                    new FaultReason("Nevalidan DateTime."));
            }
        }
    }
}
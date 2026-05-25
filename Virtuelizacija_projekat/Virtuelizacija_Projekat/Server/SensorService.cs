using Common;
using Common.Faults;
using System;
using System.Configuration;
using System.ServiceModel;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SensorService : ISensorService
    {
        private bool sessionStarted = false;
        private MeasurementFileWriter fileWriter;
        private readonly string serverDataPath;
        private int receivedSamples = 0;
        private DateTime transferStartTime;

        public SensorService()
        {
            serverDataPath = ConfigurationManager.AppSettings["serverDataPath"];

            if (string.IsNullOrWhiteSpace(serverDataPath))
            {
                serverDataPath = "data";
            }
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
            transferStartTime = DateTime.Now;

            Console.WriteLine("======================================");
            Console.WriteLine("PRENOS U TOKU...");
            Console.WriteLine("Sesija: " + meta.SessionId);
            Console.WriteLine("Vreme pocetka: " + transferStartTime);
            Console.WriteLine("======================================");

            Console.WriteLine("Sesija je pokrenuta.");
            Console.WriteLine("Meta podaci:");
            Console.WriteLine("Volume: " + meta.Volume);
            Console.WriteLine("T_DHT: " + meta.T_DHT);
            Console.WriteLine("T_BMP: " + meta.T_BMP);
            Console.WriteLine("Pressure: " + meta.Pressure);
            Console.WriteLine("DateTime: " + meta.DateTime);

            return new ServiceResponse
            {
                Ack = true,
                Message = "StartSession uspesno izvrsen. Kreirani su measurements_session.csv i rejects.csv.",
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

            Console.WriteLine("--------------------------------------");
            Console.WriteLine("PRENOS U TOKU...");
            Console.WriteLine("Primljen sample broj: " + receivedSamples);
            Console.WriteLine("Volume: " + sample.Volume);
            Console.WriteLine("T_DHT: " + sample.T_DHT);
            Console.WriteLine("T_BMP: " + sample.T_BMP);
            Console.WriteLine("Pressure: " + sample.Pressure);
            Console.WriteLine("DateTime: " + sample.DateTime);
            Console.WriteLine("--------------------------------------");

           

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

            Console.WriteLine("======================================");
            Console.WriteLine("ZAVRSEN PRENOS");
            Console.WriteLine("Ukupno primljenih uzoraka: " + receivedSamples);
            Console.WriteLine("Vreme pocetka: " + transferStartTime);
            Console.WriteLine("Vreme zavrsetka: " + transferEndTime);
            Console.WriteLine("Trajanje prenosa: " + duration.TotalSeconds.ToString("0.00") + " sekundi");
            Console.WriteLine("======================================");

            return new ServiceResponse
            {
                Ack = true,
                Message = "EndSession uspesno izvrsen. Fajlovi su zatvoreni.",
                Status = TransferStatus.COMPLETED
            };
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
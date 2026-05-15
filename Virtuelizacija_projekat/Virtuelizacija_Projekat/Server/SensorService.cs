using Common;
using Common.Faults;
using System;
using System.ServiceModel;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SensorService : ISensorService
    {
        private  bool sessionStarted = false;

        public ServiceResponse StartSession(SessionMeta meta)
        {
            ValidateMeta(meta);

            if (sessionStarted)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Sesija je vec pokrenuta." },
                    new FaultReason("Sesija je vec pokrenuta."));
            }

            sessionStarted = true;

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

            ValidateSample(sample);

            Console.WriteLine("Primljen sample:");
            Console.WriteLine("Volume: " + sample.Volume);
            Console.WriteLine("T_DHT: " + sample.T_DHT);
            Console.WriteLine("T_BMP: " + sample.T_BMP);
            Console.WriteLine("Pressure: " + sample.Pressure);
            Console.WriteLine("DateTime: " + sample.DateTime);

            return new ServiceResponse
            {
                Ack = true,
                Message = "Sample je uspesno primljen.",
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

            Console.WriteLine("Sesija je zavrsena.");

            return new ServiceResponse
            {
                Ack = true,
                Message = "EndSession uspesno izvrsen.",
                Status = TransferStatus.COMPLETED
            };
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
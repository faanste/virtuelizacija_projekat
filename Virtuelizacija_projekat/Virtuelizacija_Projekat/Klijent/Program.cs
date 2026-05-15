using Common;
using Common.Faults;
using System;
using System.ServiceModel;

namespace Klijent
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ChannelFactory<ISensorService> factory = null;
            ISensorService proxy = null;

            try
            {
                factory = new ChannelFactory<ISensorService>("SensorService");
                proxy = factory.CreateChannel();

                SessionMeta meta = new SessionMeta
                {
                    SessionId = "sesija-1",
                    Volume = "Volume",
                    T_DHT = "T_DHT",
                    T_BMP = "T_BMP",
                    Pressure = "Pressure",
                    DateTime = "DateTime"
                };

                ServiceResponse startResponse = proxy.StartSession(meta);
                PrintResponse(startResponse);

                SensorSample validSample = new SensorSample
                {
                    Volume = 45.5,
                    T_DHT = 23.4,
                    T_BMP = 24.1,
                    Pressure = 1013.25,
                    DateTime = DateTime.Now
                };

                ServiceResponse pushResponse = proxy.PushSample(validSample);
                PrintResponse(pushResponse);

                SensorSample invalidSample = new SensorSample
                {
                    Volume = 50,
                    T_DHT = 24,
                    T_BMP = 24.5,
                    Pressure = -5,
                    DateTime = DateTime.Now
                };

                Console.WriteLine();
                Console.WriteLine("Saljem nevalidan sample da testiram fault...");

                try
                {
                    proxy.PushSample(invalidSample);
                }
                catch (FaultException<ValidationFault> ex)
                {
                    Console.WriteLine("Uhvaćen ValidationFault: " + ex.Detail.Message);
                }

                ServiceResponse endResponse = proxy.EndSession();
                PrintResponse(endResponse);

                ((IClientChannel)proxy).Close();
                factory.Close();
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine("DataFormatFault: " + ex.Detail.Message);
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine("ValidationFault: " + ex.Detail.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greska na klijentu: " + ex.Message);

                if (proxy != null)
                {
                    ((IClientChannel)proxy).Abort();
                }

                if (factory != null)
                {
                    factory.Abort();
                }
            }

            Console.WriteLine();
            Console.WriteLine("Pritisni ENTER za kraj.");
            Console.ReadLine();
        }

        private static void PrintResponse(ServiceResponse response)
        {
            Console.WriteLine();
            Console.WriteLine("ACK: " + response.Ack);
            Console.WriteLine("Message: " + response.Message);
            Console.WriteLine("Status: " + response.Status);
        }
    }
}
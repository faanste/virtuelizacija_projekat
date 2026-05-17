using Common;
using Common.Faults;
using System;
using System.ServiceModel;
using System.Collections.Generic;


namespace Klijent
{
    internal class Program
    {
        static void Main(string[] args)
        {
 
            CsvLoader loader = new CsvLoader();
            List<SensorSample> csvSamples = loader.LoadFirst100Samples();
            Console.WriteLine($"Ucitano {csvSamples.Count} uzoraka iz CSV fajla.");
            Console.WriteLine();


            if (csvSamples.Count == 0)
            {
                Console.WriteLine("Nema uzoraka za slanje. Provjeri CSV fajl i putanju u App.config.");
                Console.WriteLine("Pritisni ENTER za kraj.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("=== Slanje uzoraka na server ===");
            using (SensorServiceClient client = new SensorServiceClient())
            {
                try
                {

                    SessionMeta meta = new SessionMeta
                    {
                        SessionId = "sesija-1",
                        Volume = "Volume [mV]",
                        T_DHT = "Temperature-DHT [Celsius]",
                        T_BMP = "Temperature-BMP [Celsius]",
                        Pressure = "Pressure [Hectopascal]",
                        DateTime = "Date time"
                    };

                    ServiceResponse startResponse = client.Proxy.StartSession(meta);
                    PrintResponse(startResponse);

                    for (int i = 0; i < csvSamples.Count; i++)
                    {
                        try
                        {
                            ServiceResponse pushResponse = client.Proxy.PushSample(csvSamples[i]);
                            Console.WriteLine($"[{i + 1}/{csvSamples.Count}] Sample poslan. ACK: {pushResponse.Ack}");
                        }
                        catch (FaultException<ValidationFault> ex)
                        {
                            Console.WriteLine($"[{i + 1}/{csvSamples.Count}] ValidationFault: {ex.Detail.Message}");
                        }
                        catch (FaultException<DataFormatFault> ex)
                        {
                            Console.WriteLine($"[{i + 1}/{csvSamples.Count}] DataFormatFault: {ex.Detail.Message}");
                        }
                    }
 
                    ServiceResponse endResponse = client.Proxy.EndSession();
                    PrintResponse(endResponse);

                    
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
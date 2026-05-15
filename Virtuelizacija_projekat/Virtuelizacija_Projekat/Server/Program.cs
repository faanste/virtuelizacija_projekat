using System;
using System.ServiceModel;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = null;

            try
            {
                host = new ServiceHost(typeof(SensorService));
                host.Open();

                Console.WriteLine("Server je pokrenut.");
                Console.WriteLine("Pritisni ENTER za zaustavljanje servera.");
                Console.ReadLine();

                host.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greska: " + ex.Message);

                if (host != null)
                {
                    host.Abort();
                }
            }
        }
    }
}
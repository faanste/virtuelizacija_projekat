using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Common;

namespace Klijent
{
    public class SensorServiceClient : IDisposable
    {
        private ChannelFactory<ISensorService> factory;
        private ISensorService proxy;
        private bool disposed = false;

        public ISensorService Proxy => proxy;

        public SensorServiceClient()
        {
            factory = new ChannelFactory<ISensorService>("SensorService");
            proxy = factory.CreateChannel();

            Console.WriteLine("SensorServiceClient kanal je otvoren.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); 
        }

        ~SensorServiceClient()
        {
            Dispose(false);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    try
                    {
                        if (proxy != null)
                        {
                            IClientChannel channel = (IClientChannel)proxy;

                            if (channel.State == CommunicationState.Faulted)
                            {
                                channel.Abort();
                                Console.WriteLine("[Dispose] Kanal je bio u Faulted stanju - izvrseno Abort().");
                            }
                            else
                            {
                                channel.Close();
                                Console.WriteLine("[Dispose] Kanal je ispravno zatvoren - Close().");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Dispose] Greska pri zatvaranju kanala: " + ex.Message);
                        ((IClientChannel)proxy)?.Abort();
                    }

                    try
                    {
                        if (factory != null)
                        {
                            if (factory.State == CommunicationState.Faulted)
                            {
                                factory.Abort();
                                Console.WriteLine("[Dispose] Factory je bila u Faulted stanju - izvrseno Abort().");
                            }
                            else
                            {
                                factory.Close();
                                Console.WriteLine("[Dispose] Factory je ispravno zatvorena - Close().");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Dispose] Greska pri zatvaranju factory: " + ex.Message);
                        factory?.Abort();
                    }
                }

                disposed = true;
            }
        }
    }
}

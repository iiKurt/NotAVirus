// RECEIVING

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Receive
{
    public class UDPListener
    {
        private const int listenPort = 11000;

        public UDPListener()
        {
            UdpClient listener = new UdpClient(listenPort);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);

            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for broadcast");
                    byte[] bytes = listener.Receive(ref groupEP);

                    Console.WriteLine($"Received broadcast from {groupEP} :");
                    Console.WriteLine($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                listener.Close();
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            new UDPListener();
            Console.ReadKey();
        }
    }
}

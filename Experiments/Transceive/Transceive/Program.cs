using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Transceive
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter a port to send to: ");
            int sendPort = Int32.Parse(Console.ReadLine());

            while (true)
            {
                Console.WriteLine("Enter a message: ");
                string message = Console.ReadLine();

                Send(message, sendPort);
            }

            Console.WriteLine("Enter a port to receive from:");
            int receivePort = Int32.Parse(Console.ReadLine());
            Receive(receivePort);
        }

        /// <summary>
        /// Sending
        /// </summary>

        static void Send(string message, int port = 11000)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            IPAddress broadcast = IPAddress.Parse("192.168.255.255");

            byte[] sendbuf = Encoding.ASCII.GetBytes(message);
            IPEndPoint ep = new IPEndPoint(broadcast, port);

            s.SendTo(sendbuf, ep);
            
            Console.WriteLine($"Message sent to the broadcast address ({ep.Port})");
            //Console.ReadKey();
        }

        /// <summary>
        /// Receving
        /// </summary>

        public class UDPListener
        {
            public UDPListener(int port = 11000)
            {
                UdpClient listener = new UdpClient(port);
                IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, port);

                try
                {
                    while (true)
                    {
                        Console.WriteLine("Waiting for broadcast");
                        byte[] bytes = listener.Receive(ref groupEP);

                        Console.WriteLine($"Received broadcast from {groupEP.Address} (on port {groupEP.Port}) :");
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

        static void Receive(int port)
        {
            new UDPListener(port);
            Console.ReadKey();
        }
    }
}

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Transceive
{
    class Program
    {
		static void Main(string[] args)
		{
			Console.WriteLine("Enter a port to receive from:");
			int receivePort = Int32.Parse(Console.ReadLine());
			new UDPListener(receivePort);

			Console.WriteLine("=================================");

			Console.WriteLine("Enter a port to send to: ");
			int sendPort = Int32.Parse(Console.ReadLine());

			while (true) {
				Console.WriteLine("Enter a message: ");
				string message = Console.ReadLine();

				Send(message, sendPort);
			}
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
            UdpClient client;

            public UDPListener(int port = 11000)
            {
                //Client uses as receive udp client
                client = new UdpClient(port);

                try
                {
                    client.BeginReceive(new AsyncCallback(recv), null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            //CallBack
            private void recv(IAsyncResult res)
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 8000);
                byte[] received = client.EndReceive(res, ref RemoteIpEndPoint);

                //Process codes

                Console.WriteLine("New Message: " + Encoding.UTF8.GetString(received));
                client.BeginReceive(new AsyncCallback(recv), null);
            }
        }
    }
}

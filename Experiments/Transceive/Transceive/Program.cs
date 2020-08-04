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

			Broadcast broadcast = new Broadcast(getLocalIP(), receivePort);
			broadcast.NewBroadcast += Broadcast_NewBroadcast;

			Console.WriteLine("=================================");

			Console.WriteLine("Enter a port to send to: ");
			int sendPort = Int32.Parse(Console.ReadLine());

			while (true) {
				Console.WriteLine("Enter a message: ");
				string message = Console.ReadLine();

				broadcast.Send(message, sendPort);
			}
		}

		private static void Broadcast_NewBroadcast(object sender, NewBroadcastEventArgs e)
		{
			Console.WriteLine($"New Message: {e.message}");
		}

		private static IPAddress getLocalIP()
		{
			IPHostEntry host;
			host = Dns.GetHostEntry(Dns.GetHostName());

			foreach (IPAddress ip in host.AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					return ip;
				}
			}

			return IPAddress.Parse("127.0.0.1");
			//return "10.0.2.15";
		}

		/// <summary>
		/// Pretend this is in a seperate file
		/// </summary>

		public class Broadcast
        {
			// EventThing
			public event EventHandler<NewBroadcastEventArgs> NewBroadcast;

			UdpClient client;
			IPAddress selfIP;
			int port;

            public Broadcast(IPAddress selfIP, int port = 11000)
            {
				this.selfIP = selfIP;
				this.port = port;
                //Client uses as receive udp client
                client = new UdpClient(port);

                try
                {
                    client.BeginReceive(new AsyncCallback(OnBroadcastMessage), null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

			// EventThing
			protected virtual void OnNewBroadcast(NewBroadcastEventArgs e)
			{
				EventHandler<NewBroadcastEventArgs> handler = NewBroadcast;
				handler?.Invoke(this, e);
			}

			//This is called when a message is received (before any events are called)
			private void OnBroadcastMessage(IAsyncResult res)
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, port);
                byte[] received = client.EndReceive(res, ref RemoteIpEndPoint);
				
				// Begin receiving A$AP
				client.BeginReceive(new AsyncCallback(OnBroadcastMessage), null);

				//Process the message
				if (!RemoteIpEndPoint.Address.Equals(selfIP)) // message is from someone else
				{
					NewBroadcastEventArgs args = new NewBroadcastEventArgs();
					args.message = Encoding.UTF8.GetString(received);
					NewBroadcast(this, args); // raise event
				}
			}

			public void Send(string message, int port)
			{
				Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

				IPAddress broadcast = IPAddress.Parse("192.168.255.255");

				byte[] sendbuf = Encoding.ASCII.GetBytes(message);
				IPEndPoint ep = new IPEndPoint(broadcast, port);

				s.SendTo(sendbuf, ep);

				Console.WriteLine($"Message sent to the broadcast address ({ep.Port})");
				//Console.ReadKey();
			}
		}

		// EventThing
		public class NewBroadcastEventArgs : EventArgs
		{
			public string message;
		}
	}
}

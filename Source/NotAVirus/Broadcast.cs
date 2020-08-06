using System;
using System.Net;
using System.Net.Sockets;

namespace NotAVirus
{
	public class Broadcast
	{
		// EventThing
		public event EventHandler<NewBroadcastEventArgs> Join;
		public event EventHandler<NewBroadcastEventArgs> Discovery;

		UdpClient client;
		IPAddress selfIP;
		ushort port;

		public Broadcast(IPAddress selfIP, ushort port = 11000)
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
		protected virtual void OnJoin(NewBroadcastEventArgs e)
		{
			EventHandler<NewBroadcastEventArgs> handler = Join;
			handler?.Invoke(this, e);
		}
		
		protected virtual void OnDiscovery(NewBroadcastEventArgs e)
		{
			EventHandler<NewBroadcastEventArgs> handler = Discovery;
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
				args.message = new RemoteMessage(received);

				switch (args.message.Event)
				{
					case Event.Join:
						Join(this, args); // raise event
						break;
					case Event.Discovery:
						Discovery(this, args);
						break;
				}
			}
		}

		public void Send(RemoteMessage message, int port)
		{
			Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

			IPAddress broadcast = IPAddress.Parse("192.168.255.255");
			IPEndPoint ep = new IPEndPoint(broadcast, port);

			s.SendTo(message.Serialize(), ep);

			Console.WriteLine($"Message sent to the broadcast address ({ep.Port})");
		}
	}

	// EventThing
	public class NewBroadcastEventArgs : EventArgs
	{
		public RemoteMessage message;
	}
}

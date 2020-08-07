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
		LocalClient self;
		ushort port;

		public Broadcast(LocalClient self, ushort port = 11000)
		{
			this.self = self;
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
			IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, port);
			byte[] received = client.EndReceive(res, ref groupEP);

			// Begin receiving A$AP
			client.BeginReceive(new AsyncCallback(OnBroadcastMessage), null);

			//Process the message
			if (!groupEP.Address.Equals(self.IP)) // message is from someone else
			{
				NewBroadcastEventArgs args = new NewBroadcastEventArgs();
				args.message = new RemoteMessage(received);
				
				// TODO: seems dodgy, fix it.
				if (args.message.Event != Event.Join && args.message.Event != Event.Discovery)
				{
					return; // nuh uh - nope. should be a message directly to clients
				}
				// creating another new localclient seems real dodgy
				args.message.Sender = new RemoteClient(self, groupEP.Address, port, args.message.Words);

				switch (args.message.Event)
				{
					// raise events
					case Event.Join:
						Join(this, args);
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

			//IPAddress broadcast = IPAddress.Parse("192.168.255.255");
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);

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

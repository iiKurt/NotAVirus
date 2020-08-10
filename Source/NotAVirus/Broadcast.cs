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
		LocalClient local;
		ushort port;
        IPEndPoint groupEP;

        public Broadcast(LocalClient local, ushort port = 11000)
		{
			this.local = local;
			this.port = port;
            groupEP = new IPEndPoint(IPAddress.Any, port);

            //Client uses as receive udp client
            client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(groupEP);

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
			byte[] received = client.EndReceive(res, ref groupEP);

			// Begin receiving A$AP
			client.BeginReceive(new AsyncCallback(OnBroadcastMessage), null);

			//Process the message
			if (!groupEP.Address.Equals(local.EP.Address)) // message is from someone else
			{
				NewBroadcastEventArgs args = new NewBroadcastEventArgs();
				args.message = new RemoteMessage(received);
				
				// TODO: seems dodgy, fix it.
				if (args.message.Event != Event.Join && args.message.Event != Event.Discovery)
				{
					return; // nuh uh - nope. should be a message directly to clients
				}
				// creating another new remoteclient seems real dodgy
				args.message.Sender = new RemoteClient(local, groupEP.Address, port, args.message.Words);

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
                Console.WriteLine(args.message.Words);
                Console.WriteLine(args.message.Sender.Name);
            }
		}

		public void Send(RemoteMessage message, int port)
		{
			Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			
			// convert X.X.X.X -> X.X.255.255
			byte[] ip = local.EP.Address.GetAddressBytes();
			ip[2] = 255;
			ip[3] = 255;
			IPEndPoint ep = new IPEndPoint(new IPAddress(ip), port);

			s.SendTo(message.Serialize(), ep);
		}
	}

	// EventThing
	public class NewBroadcastEventArgs : EventArgs
	{
		public RemoteMessage message;
	}
}

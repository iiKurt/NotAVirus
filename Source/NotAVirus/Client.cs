using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NotAVirus
{
	public abstract class Client
	{
		public string Name { get; set; }
	}

	public class LocalClient : Client
	{
		public IPEndPoint EP; // cannot be property or Socket receiving doesn't like it

		public LocalClient(ushort port)
		{
			// Get the local IP
			// Could 
			IPHostEntry host;
			host = Dns.GetHostEntry(Dns.GetHostName());

			IPAddress theIP = IPAddress.Loopback; // in case we can't find an IP
												  // TODO: could the IPAddress.Loopback be used instead of all this crap? v v v
			foreach (IPAddress ip in host.AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					theIP = ip;
					break;
				}
			}

			EP = new IPEndPoint(theIP, port);
		}
	}

	public class RemoteClient : Client
	{
		private UdpClient client;
		private IPEndPoint localEP;
		private IPEndPoint remoteEP;

		public event EventHandler<NewMessageEventArgs> NewMessage;
        public event EventHandler<EventArgs> Leave;

		public RemoteClient(LocalClient localBinding, IPAddress ip, ushort port = 3012, string name = "Other")
		{
			Name = name;
			localEP = localBinding.EP;
			remoteEP = new IPEndPoint(ip, port);

			// https://stackoverflow.com/questions/9120050/connecting-two-udp-clients-to-one-port-send-and-receive
			client = new UdpClient();
			client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			client.Client.Bind(localEP);

			client.BeginReceive(new AsyncCallback(MessageCallBack), null);
		}

		public void Close()
        {
			client.Close();
        }

		protected virtual void OnNewMessage(NewMessageEventArgs e)
		{
			EventHandler<NewMessageEventArgs> handler = NewMessage;
			handler?.Invoke(this, e);
		}

		private void MessageCallBack(IAsyncResult result)
		{
			byte[] receivedData = { };
			try
			{
				receivedData = client.EndReceive(result, ref remoteEP);
			}
			catch (Exception ex)
			{
				//if (ex.SocketErrorCode == SocketError.ConnectionReset) //connection was forcibly closed by the remote host
				if (ex is SocketException)
				{
					Leave(this, new EventArgs());
					return;
				}
				else if (ex is ObjectDisposedException)
				{
					// TODO: somehow purge all async taskes rather than waiting for it to error out
					return; // udpClient has been closed
				}
				else
				{
					throw;
				}
			}
			RemoteMessage message = new RemoteMessage(receivedData);
			message.Sender = this;

			// begin receving as soon as possible
			client.BeginReceive(new AsyncCallback(MessageCallBack), null);
			
			switch (message.Event)
			{
				case Event.Leave:
					Leave(this, new EventArgs());
					break;
				case Event.Message:
					NewMessageEventArgs args = new NewMessageEventArgs();
					args.message = message;
					NewMessage(this, args);
					break;
				// what if a client sends us a private join or discovery message..? they dumb dumb
			}
		}

		public void Send(RemoteMessage message)
		{
			byte[] msg = message.Serialize();

			//client.Send(msg, msg.Length);
			// would be nice to somehow avoid creating a new socket just to send a message
			// I hear that sometimes people create a persistent second udpclient just for sending/receiving
			Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

			s.SendTo(msg, remoteEP);
		}
	}

	public class NewMessageEventArgs : EventArgs
	{
		public RemoteMessage message;
	}
}

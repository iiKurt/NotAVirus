using System;
using System.Net;
using System.Net.Sockets;

namespace NotAVirus
{
    public abstract class Client
    {
        public string Name { get; set; }
        public EndPoint EndPoint; // cannot be property or Socket receiving doesn't like it
    }

    public class LocalClient : Client
    {
        public IPAddress IP { get; private set; }

        public LocalClient(ushort port)
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());

            IP = IPAddress.Parse("127.0.0.1"); // in case we can't find an IP
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    IP = ip;
                    break;
                }
            }

            EndPoint = new IPEndPoint(IP, port);
        }
    }

	public class RemoteClient : Client
	{
		private Socket Socket;

		public event EventHandler<NewMessageEventArgs> NewMessage;
        public event EventHandler<EventArgs> Leave;

        public RemoteClient(LocalClient binding, IPAddress ip, ushort port = 3012, string name = "Other")
		{
			Name = name;
			EndPoint = new IPEndPoint(ip, port);

			Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

			Socket.Bind(binding.EndPoint);
			Socket.Connect(EndPoint);

			byte[] buffer = new byte[1500];
			Socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref EndPoint, new AsyncCallback(MessageCallBack), buffer);
		}

        public void Close()
        {
            Socket.Shutdown(SocketShutdown.Both);
            //Socket.Disconnect(true);
            Socket.Close();
        }

		protected virtual void OnNewMessage(NewMessageEventArgs e)
		{
			EventHandler<NewMessageEventArgs> handler = NewMessage;
			handler?.Invoke(this, e);
		}

		private void MessageCallBack(IAsyncResult result)
		{
            int size = 0;
            try
            {
                size = Socket.EndReceiveFrom(result, ref EndPoint);
            }
            catch (Exception ex)
            {
                //if (ex.SocketErrorCode == SocketError.ConnectionReset) //connection was forcibly closed by the remote host
                if (ex is SocketException)
                {
                    Leave(this, new EventArgs());
                }
                else if (ex is ObjectDisposedException)
                {
                    return; // socket is closed
                }
                else
                {
                    throw;
                }
            }
			
			if (size > 0) // there's a message
			{
				byte[] receivedData = new byte[1464];

				receivedData = (byte[])result.AsyncState;

				RemoteMessage message = new RemoteMessage(receivedData);
				message.Sender = this;

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

            // could this be moved to before message processing?
			byte[] buffer = new byte[1500];
			Socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref EndPoint, new AsyncCallback(MessageCallBack), buffer);
		}

		public void Send(RemoteMessage message)
		{
			byte[] msg = message.Serialize();

			Socket.Send(msg);
		}
	}

	public class NewMessageEventArgs : EventArgs
	{
		public RemoteMessage message;
	}
}

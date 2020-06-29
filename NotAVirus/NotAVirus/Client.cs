using System;
using System.Net;
using System.Net.Sockets;

namespace NotAVirus
{
	public class Client
	{
		public string Name { get; set; }
		private Socket Socket;
		public EndPoint EndPoint;

		public event EventHandler<NewMessageEventArgs> NewMessage;

		public Client(EndPoint Binding, IPAddress IP, int Port = 3012, string Name = "Other")
		{
			this.Name = Name;
			this.EndPoint = new IPEndPoint(IP, Port);

			Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

			Socket.Bind(Binding);
			Socket.Connect(EndPoint);

			byte[] buffer = new byte[1500];
			Socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref EndPoint, new AsyncCallback(MessageCallBack), buffer);
		}

		protected virtual void OnNewMessage(NewMessageEventArgs e)
		{
			EventHandler<NewMessageEventArgs> handler = NewMessage;
			handler?.Invoke(this, e);
		}

		private void MessageCallBack(IAsyncResult result)
		{
			int size = Socket.EndReceiveFrom(result, ref EndPoint);

			if (size > 0) // there's a message
			{
				byte[] receivedData = new byte[1464];

				receivedData = (byte[])result.AsyncState;

				RemoteMessage message = new RemoteMessage(receivedData);

				NewMessageEventArgs args = new NewMessageEventArgs();
				args.message = message;
				NewMessage(this, args);
			}

			byte[] buffer = new byte[1500];
			Socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref EndPoint, new AsyncCallback(MessageCallBack), buffer);
		}

		public void SendMessage(RemoteMessage message)
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

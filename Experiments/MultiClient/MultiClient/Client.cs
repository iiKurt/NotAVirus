using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MultiClient
{
    public abstract class Client
    {
        public string Name { get; set; }
    }

    public class LocalClient : Client
    {
        public IPAddress IP { get; private set; }
		public IPEndPoint EndPoint; // cannot be property or Socket receiving doesn't like it

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
		private UdpClient client;
		private LocalClient binding;

		public event EventHandler<NewMessageEventArgs> NewMessage;
        public event EventHandler<EventArgs> Leave;

        public RemoteClient(LocalClient binding, IPAddress ip, ushort port = 3012, string name = "Other")
		{
			this.Name = name;
			this.binding = binding;

			client = new UdpClient(port);
			
			client.BeginReceive(new AsyncCallback(MessageCallBack), null);
		}

		protected virtual void OnNewMessage(NewMessageEventArgs e)
		{
			EventHandler<NewMessageEventArgs> handler = NewMessage;
			handler?.Invoke(this, e);
		}

		private void MessageCallBack(IAsyncResult result)
		{
			byte[] receivedData = client.EndReceive(result, ref binding.EndPoint);
			string message = Name + ": " + Encoding.ASCII.GetString(receivedData);
				
			NewMessageEventArgs args = new NewMessageEventArgs();
			args.message = message;
			NewMessage(this, args);

			client.BeginReceive(new AsyncCallback(MessageCallBack), null);
		}

		public void Send(string message)
		{
			byte[] msg = Encoding.ASCII.GetBytes(message);

			client.Send(msg, msg.Length);
		}
	}

	public class NewMessageEventArgs : EventArgs
	{
		public string message;
	}
}

using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace NotAVirus
{
	public class Broadcast
	{
		public UdpClient Client;
		public Socket Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		public IPEndPoint EP;

		public Broadcast(IPAddress localIP, short port)
		{
			Client = new UdpClient(port);

			RemoteMessage msg = new RemoteMessage(Event.Join);

			string[] ip = localIP.ToString().Split('.');
			string ip2 = ip[0] + "." + ip[1] + ".255.255";
			
			byte[] sendbuf = msg.Serialize();
			EP = new IPEndPoint(IPAddress.Parse(ip2), port);

			Socket.SendTo(sendbuf, EP);
            //broadcast.BeginReceiveFrom(sendbuf, 0, sendbuf.Length, SocketFlags.None, ref ep, new AsyncCallback(OnBroadcastMessage), sendbuf);

            MessageBox.Show("sending broadcast");

			try
			{
                UdpClient listener = new UdpClient(port);
                IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, port);

                while (true)
				{
					Console.WriteLine("Waiting for broadcast");
					byte[] bytes = listener.Receive(ref groupEP);

					Console.WriteLine($"Received broadcast from {groupEP} :");
				}
			}
			catch (SocketException e)
			{
				Console.WriteLine(e);
			}
			finally
			{
				Client.Close();
			}

            MessageBox.Show("done broadcast, now checking");
		}

		public void OnBroadcastMessage(IAsyncResult result)
		{
			MessageBox.Show("uhbguribngr");
			// this is running in an async thread
			// so we need to invoke the UI
			// TODO: check how I did this on the old versions
			//Dispatcher.BeginInvoke(new Action(() =>
			NewBroadcastMessage();
			//));
		}

		public void NewBroadcastMessage()
		{
			// only should check if event.join or event.discovery...
			// somehow get IPs...
			MessageBox.Show(EP.Address.ToString());
			/*switch ()
			{
				case Event.Join:
					LocalMessage msg = new LocalMessage();
					msg.Contents = "Got resposne from " + message.Sender;
					messages.Add(msg);
					break;
			}*/
		}
	}
}

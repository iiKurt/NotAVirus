// I don't even know how this even works but it does, so don't touch it or the whole thing breaks
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MultiClient
{
    public class Broadcast
    {
        // EventThing
        public event EventHandler<NewBroadcastEventArgs> Message;

        UdpClient client;
        IPEndPoint localEP;
        IPEndPoint remoteEP;

        public Broadcast(IPAddress ip, ushort port = 11000)
        {
            localEP = new IPEndPoint(ip, port);
            remoteEP = new IPEndPoint(IPAddress.Any, port);

            //Client uses as receive udp client
            client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(remoteEP);

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
        protected virtual void OnMessage(NewBroadcastEventArgs e)
        {
            EventHandler<NewBroadcastEventArgs> handler = Message;
            handler?.Invoke(this, e);
        }

        //This is called when a message is received (before any events are called)
        private void OnBroadcastMessage(IAsyncResult res)
        {
            byte[] received = client.EndReceive(res, ref remoteEP);

            // Begin receiving A$AP
            client.BeginReceive(new AsyncCallback(OnBroadcastMessage), null);

            //Process the message
            if (!remoteEP.Address.Equals(localEP.Address)) // message is from someone else
            {
                NewBroadcastEventArgs args = new NewBroadcastEventArgs();
                args.message = Encoding.ASCII.GetString(received);
                
                // raise events
                Message(this, args);
            }
        }

        public void Send(string message)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // convert X.X.X.X -> X.X.255.255
            byte[] ip = localEP.Address.GetAddressBytes();
            ip[2] = 255;
            ip[3] = 255;
            IPEndPoint ep = new IPEndPoint(new IPAddress(ip), localEP.Port);

            s.SendTo(Encoding.ASCII.GetBytes(message), ep);
        }
    }

    // EventThing
    public class NewBroadcastEventArgs : EventArgs
    {
        public string message;
    }
}

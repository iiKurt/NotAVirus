// I don't even know how this even works but it does, so don't touch it or the whole thing breaks
using System;
using System.Net;
using System.Net.Sockets;

namespace PolarBear
{
    public class Broadcast
    {
        // EventThing
        public event EventHandler<NewBroadcastEventArgs> Join;
        public event EventHandler<NewBroadcastEventArgs> Discovery;
        public event EventHandler<NewBroadcastEventArgs> Leave;
        public event EventHandler<NewBroadcastEventArgs> Message;

        UdpClient client;
        IPEndPoint localEP;
        IPEndPoint remoteEP;

        public Broadcast(IPEndPoint EP)
        {
            localEP = EP;
            remoteEP = new IPEndPoint(IPAddress.Any, EP.Port);

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
        protected virtual void OnLeave(NewBroadcastEventArgs e)
        {
            EventHandler<NewBroadcastEventArgs> handler = Leave;
            handler?.Invoke(this, e);
        }
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
                args.message = new RemoteMessage(received);

                switch (args.message.Event)
                {
                    case Event.Join:
                        OnJoin(args);
                        break;
                    case Event.Discovery:
                        OnDiscovery(args);
                        break;
                    case Event.Leave:
                        OnLeave(args);
                        break;
                    case Event.Message:
                        OnMessage(args);
                        break;
                }
            }
        }

        public void Send(RemoteMessage message)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // convert X.X.X.X -> X.X.255.255
            byte[] ip = localEP.Address.GetAddressBytes();
            ip[2] = 255;
            ip[3] = 255;
            IPEndPoint ep = new IPEndPoint(new IPAddress(ip), localEP.Port);

            s.SendTo(message.Serialize(), ep);
        }
    }

    // EventThing
    public class NewBroadcastEventArgs : EventArgs
    {
        public RemoteMessage message;
    }
}

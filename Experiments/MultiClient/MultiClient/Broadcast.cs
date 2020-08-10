using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MultiClient
{
    public class Broadcast
    {
        // EventThing
        public event EventHandler<NewBroadcastEventArgs> Join;
        public event EventHandler<NewBroadcastEventArgs> Discovery;
        public event EventHandler<NewBroadcastEventArgs> Message;

        IPEndPoint local;
        IPEndPoint remote;
        UdpClient sender;
        UdpClient receiver; // TODO: would be nice to have this as socket

        public Broadcast(IPAddress ip, ushort port = 11000)
        {
            local = new IPEndPoint(ip, port);

            // convert X.X.X.X -> X.X.255.255
            byte[] broadcast = ip.GetAddressBytes();
            broadcast[2] = 255;
            broadcast[3] = 255;
            remote = new IPEndPoint(new IPAddress(broadcast), port);

            sender = new UdpClient();
            sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            sender.Client.Bind(local);

            // https://stackoverflow.com/questions/9120050/connecting-two-udp-clients-to-one-port-send-and-receive
            receiver = new UdpClient();
            receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            receiver.Client.Bind(local);

            try
            {
                receiver.BeginReceive(new AsyncCallback(OnBroadcastMessage), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        #region Events
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
        protected virtual void OnMessage(NewBroadcastEventArgs e)
        {
            EventHandler<NewBroadcastEventArgs> handler = Message;
            handler?.Invoke(this, e);
        }
        #endregion

        //This is called when a message is received (before any events are called)
        private void OnBroadcastMessage(IAsyncResult res)
        {
            byte[] received = receiver.EndReceive(res, ref remote);

            // Begin receiving A$AP
            receiver.BeginReceive(new AsyncCallback(OnBroadcastMessage), null);

            //Process the message
            if (!remote.Address.Equals(local.Address)) // ensure message is from someone else
            {
                NewBroadcastEventArgs args = new NewBroadcastEventArgs();
                args.message = Encoding.ASCII.GetString(received);
                OnMessage(args); // raise event
            }
        }

        public void Send(string message)
        {
            byte[] msg = Encoding.ASCII.GetBytes(message);
            sender.Send(msg, msg.Length, remote);
        }
    }

    // EventThing
    public class NewBroadcastEventArgs : EventArgs
    {
        public string message;
    }
}

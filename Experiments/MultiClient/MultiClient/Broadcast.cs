using System;
using System.Net;
using System.Net.Sockets;

namespace MultiClient
{
    public class Broadcast
    {
        // EventThing
        public event EventHandler<NewBroadcastEventArgs> Join;
        public event EventHandler<NewBroadcastEventArgs> Discovery;

        public Broadcast(LocalClient local, ushort port = 11000)
        {

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
        #endregion

        //This is called when a message is received (before any events are called)
        private void OnBroadcastMessage(IAsyncResult res)
        {

        }

        public void Send(string message)
        {

        }
    }

    // EventThing
    public class NewBroadcastEventArgs : EventArgs
    {
        public string message;
    }
}

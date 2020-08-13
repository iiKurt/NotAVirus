using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace PolarBear
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Broadcast broadcast;
        const ushort port = 11000;

        ObservableCollection<Message> messages = new ObservableCollection<Message>();

        public MainWindow()
        {
            InitializeComponent();

            messagesListBox.ItemsSource = messages;

            string[] verbs = { "Fast", "Slow", "Agile", "Nimble", "Bad", "Broken" };
            string[] nouns = { "Bear", "Octopus", "Flamingo", "Optical Fibre" };

            nameTextBox.Text = $"{verbs[0]}  {nouns[0]}";
        }

        #region Helpers
        private IPEndPoint getIP()
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

            return new IPEndPoint(theIP, port);
        }
        #endregion

        #region WPF
        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                broadcast = new Broadcast(getIP());
                broadcast.Join += Broadcast_Join;
                broadcast.Discovery += Broadcast_Discovery;
                broadcast.Message += Broadcast_Message;
                broadcast.Leave += Broadcast_Leave;

                RemoteMessage msg = new RemoteMessage();
                msg.Contents = nameTextBox.Text;
                msg.Event = Event.Join;
                broadcast.Send(msg);
            }
            catch { } // TODO: handle stuff
        }
        // send a message
        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            sendTheMessage();
        }
        private void composeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                sendTheMessage();
            }
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            try // commented out for testing
            {
                // broadcast that we are online

                broadcast = new Broadcast(self.EP.Address, port);
                broadcast.Join += OnBroadcastJoin;
                broadcast.Discovery += OnBroadcastDiscovery;
                broadcast.Message += OnBroadcastMessage;
                broadcast.Leave += OnBroadcastLeave;

                RemoteMessage join = new RemoteMessage(nameTextBox.Text);
                join.Event = Event.Join;
                broadcast.Send(join);

                /*clients.Add(new RemoteClient(self, IPAddress.Parse("192.168.1.10"), 3012, "test1"));
				clients.Add(new RemoteClient(self, IPAddress.Parse("192.168.1.11"), 3012, "test2"));
				clients.Add(new RemoteClient(self, IPAddress.Parse("192.168.1.12"), 3012, "test3"));*/

                // we are 'connected'

                connectButton.IsEnabled = false;
                connectButton.Content = "Connected";

                nameLabel.IsEnabled = false;
                nameTextBox.IsEnabled = false;

                sendButton.IsEnabled = true;
                composeTextBox.IsEnabled = true;
                composeTextBox.Focus();
            }
            catch (SocketException ex)
            {
                MessageBox.Show(ex.ToString());
                throw ex; // for debugging
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!connected)
                return;

            //MessageBox.Show("goodbye");
            try
            {
                RemoteMessage message = new RemoteMessage(""); //we are the sender
                message.Event = Event.Leave;

                broadcast.Send(message);
                /*for (int i = 0; i < clients.Count; i++)
				{
					clients[i].Send(message);
				}*/
            }
            catch (SocketException ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // not really a WPF event
        private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        { // scroll any new items into view
            messagesListBox.SelectedIndex = messagesListBox.Items.Count - 1;
            messagesListBox.ScrollIntoView(messagesListBox.SelectedItem);
        }
        #endregion

        #region Broadcast
        public void OnBroadcastJoin(object sender, NewBroadcastEventArgs e) =>
            Dispatcher.BeginInvoke(new Action(() =>
                Broadcast_Join()
            ));
        private void Broadcast_Join()
        {
            throw new NotImplementedException();
        }

        public void OnBroadcastMessage(object sender, NewBroadcastEventArgs e) =>
            Dispatcher.BeginInvoke(new Action(() =>
                Broadcast_Message()
            ));
        private void Broadcast_Message()
        {
            throw new NotImplementedException();
        }

        public void OnBroadcastDiscovery(object sender, NewBroadcastEventArgs e) =>
            Dispatcher.BeginInvoke(new Action(() =>
                Broadcast_Discovery(e.message.Contents)
            ));
        private void Broadcast_Discovery(string clientName)
        {
            throw new NotImplementedException();
        }

        public void OnBroadcastLeave(object sender, NewBroadcastEventArgs e) =>
            Dispatcher.BeginInvoke(new Action(() =>
                Broadcast_Leave()
            ));
        private void Broadcast_Leave()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}

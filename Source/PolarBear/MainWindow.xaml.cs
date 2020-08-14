using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;

namespace PolarBear
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool connected = false;
        Broadcast broadcast;
        const ushort port = 11000;

        ObservableCollection<Message> messages = new ObservableCollection<Message>();

        public MainWindow()
        {
            InitializeComponent();

            messagesListBox.ItemsSource = messages;

            string[] verbs = { "Fast", "Slow", "Agile", "Nimble", "Bad", "Broken", "Purple" };
            string[] nouns = { "Bear", "Octopus", "Flamingo", "Optical Fibre", "Icecream" };

            Random rng = new Random();

            nameTextBox.Text = $"{verbs[rng.Next(0, verbs.Length)]} {nouns[rng.Next(0, nouns.Length)]}";
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
        
        private void sendTheMessage()
        {
            try
            {
                RemoteMessage msg = new RemoteMessage();
                msg.Contents = $"{nameTextBox.Text}: {composeTextBox.Text}";
                msg.Event = Event.Message;
                broadcast.Send(msg);
            }
            catch { } // TODO: catch relevant exceptions
        }
        #endregion

        #region WPF
        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                broadcast = new Broadcast(getIP());
                broadcast.Join += OnBroadcastJoin;
                broadcast.Discovery += OnBroadcastDiscovery;
                broadcast.Message += OnBroadcastMessage;
                broadcast.Leave += OnBroadcastLeave;

                // broadcast that we are online
                RemoteMessage msg = new RemoteMessage();
                msg.Contents = nameTextBox.Text;
                msg.Event = Event.Join;
                broadcast.Send(msg);

                // we are 'connected'
                connected = true;
                connectButton.IsEnabled = false;
                connectButton.Content = "Connected";

                nameLabel.IsEnabled = false;
                nameTextBox.IsEnabled = false;

                sendButton.IsEnabled = true;
                composeTextBox.IsEnabled = true;
                composeTextBox.Focus();
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

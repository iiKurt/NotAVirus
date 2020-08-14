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

        ObservableCollection<Client> clients = new ObservableCollection<Client>();
        ObservableCollection<Message> messages = new ObservableCollection<Message>();

        public MainWindow()
        {
            InitializeComponent();

            clientsListBox.ItemsSource = clients;
            messagesListBox.ItemsSource = messages;
            messages.CollectionChanged += Messages_CollectionChanged;

            string[] adjectives = { "Fast", "Slow", "Agile", "Nimble", "Bad", "Broken", "Purple", "Sticky", "Inanimate" };
            string[] nouns = { "Bear", "Octopus", "Flamingo", "Optical Fibre", "Icecream", "Stick", "Carbon Rod" };

            Random rng = new Random();

            nameTextBox.Text = $"{adjectives[rng.Next(0, adjectives.Length)]} {nouns[rng.Next(0, nouns.Length)]}";
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
            if (composeTextBox.Text == "")
                return;

            try
            {
                RemoteMessage msg = new RemoteMessage($"{nameTextBox.Text}: {composeTextBox.Text}");
                broadcast.Send(msg);

                msg.Contents = $"You: {composeTextBox.Text}";
                messages.Add(msg);

                composeTextBox.Clear();
                sendButton.IsEnabled = false;
            }
            catch { } // TODO: catch relevant exceptions
        }

        private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        { // scroll any new items into view
            messagesListBox.SelectedIndex = messagesListBox.Items.Count - 1;
            messagesListBox.ScrollIntoView(messagesListBox.SelectedItem);
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
                RemoteMessage msg = new RemoteMessage(nameTextBox.Text, Event.Join);
                broadcast.Send(msg);

                // we are 'connected'
                connected = true;
                connectButton.IsEnabled = false;
                connectButton.Content = "Connected";

                nameLabel.IsEnabled = false;
                nameTextBox.IsEnabled = false;
                
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
            switch (e.Key)
            {
                case Key.Return:
                    sendTheMessage();
                    break;
            }
        }
        
        private void composeTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (composeTextBox.Text != "")
                sendButton.IsEnabled = true;
            else
                sendButton.IsEnabled = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!connected)
                return;
            
            try
            {
                RemoteMessage message = new RemoteMessage(nameTextBox.Text, Event.Leave);
                broadcast.Send(message);
            }
            catch (SocketException ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        #endregion

        #region Broadcast
        public void OnBroadcastJoin(object sender, NewBroadcastEventArgs e) =>
            Dispatcher.BeginInvoke(new Action(() =>
                Broadcast_Join(new RemoteClient(e.message.Contents))
            ));
        private void Broadcast_Join(RemoteClient client)
        {
            clients.Add(client);
            messages.Add(new InternalMessage($"{client.Name} has joined"));
            // broadcast that we are here
            RemoteMessage msg = new RemoteMessage(nameTextBox.Text, Event.Discovery);
            broadcast.Send(msg);
        }

        public void OnBroadcastMessage(object sender, NewBroadcastEventArgs e) =>
            Dispatcher.BeginInvoke(new Action(() =>
                Broadcast_Message(e.message)
            ));
        private void Broadcast_Message(RemoteMessage message)
        {
            messages.Add(message);
        }

        public void OnBroadcastDiscovery(object sender, NewBroadcastEventArgs e) =>
            Dispatcher.BeginInvoke(new Action(() =>
                Broadcast_Discovery(new RemoteClient(e.message.Contents))
            ));
        private void Broadcast_Discovery(RemoteClient client)
        {
            // this discovery request may not be directed towards us.
            // someone else could have joined, and people respond with a discovery request, we will receive one -- even if we didn't ask for it
            // ideally a discovery request would be directly messaged towards a client

            foreach (RemoteClient c in clients)
            {
                // client already exists so we don't need to add it
                if (c.Name == client.Name) // TODO: client.Equals()?
                {
                    return;
                }
            }
            clients.Add(client);
        }

        public void OnBroadcastLeave(object sender, NewBroadcastEventArgs e) =>
            Dispatcher.BeginInvoke(new Action(() =>
                Broadcast_Leave(new RemoteClient(e.message.Contents))
            ));
        private void Broadcast_Leave(RemoteClient client)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].Name == client.Name) // TODO: client.Equals()?
                {
                    clients.RemoveAt(i);
                    messages.Add(new InternalMessage($"{client.Name} has left"));
                }
            }
        }
        #endregion

        // TODOS:
        // Direct client requests events would be:
        // Discovery, Message, Leave
        // Broadcast request events would be:
        // Join
    }
}

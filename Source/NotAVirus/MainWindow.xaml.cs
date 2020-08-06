using System;
using System.Windows;
using System.Collections.ObjectModel;

using System.Net;
using System.Net.Sockets;
using System.Windows.Input;

namespace NotAVirus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		const short port = 3012;

		EndPoint epLocal;

		// has to be observable as WPF needs to know when it updates
		ObservableCollection<Client> clients = new ObservableCollection<Client>();
		ObservableCollection<Message> messages = new ObservableCollection<Message>();

		Broadcast broadcast;

		public MainWindow()
        {
            InitializeComponent();
			messagesListBox.ItemsSource = messages;
            clientsListBox.ItemsSource = clients;

            messages.CollectionChanged += Messages_CollectionChanged;
        }

        private void Messages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        { // scroll any new items into view
            messagesListBox.SelectedIndex = messagesListBox.Items.Count - 1;
            messagesListBox.ScrollIntoView(messagesListBox.SelectedItem);
        }

        // TODO: check how I did this on the old versions
        private IPAddress getLocalIP()
		{
			IPHostEntry host;
			host = Dns.GetHostEntry(Dns.GetHostName());

			foreach (IPAddress ip in host.AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					return ip;
				}
			}

			return IPAddress.Parse("127.0.0.1");
			//return "10.0.2.15";
		}

		// called when a message is received
		public void OnMessage(object sender, NewMessageEventArgs e) =>
			Dispatcher.BeginInvoke(new Action(() =>
                Client_NewMessage((Client)sender, e.message)
			));
		
		// which actually just calls this
		public void Client_NewMessage(Client from, RemoteMessage message)
		{
			try
			{
				switch (message.Event)
				{
					case Event.Message:
						messages.Add(message);
						break;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void sendMessage()
		{
			try
			{
				RemoteMessage message = new RemoteMessage(composeTextBox.Text, nameTextBox.Text); //we are the sender

				for (int i = 0; i < clients.Count; i++)
				{
					clients[i].SendMessage(message);
				}

				message.Sender = "You";
				messages.Add(message);
				composeTextBox.Clear();
			}
			catch (SocketException ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

        #region WPF Events

        // send a message
        private void sendButton_Click(object sender, RoutedEventArgs e)
		{
			sendMessage();
		}

		private void connectButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				epLocal = new IPEndPoint(getLocalIP(), port);

				// broadcast that we are online
				broadcast = new Broadcast(getLocalIP(), port);
                broadcast.NewBroadcast += OnBroadcast;

                RemoteMessage msg = new RemoteMessage(getLocalIP().MapToIPv4().ToString(), nameTextBox.Text);
                msg.Event = Event.Join;

                broadcast.Send(msg, port);

                // we are 'connected'

                connectButton.IsEnabled = false;
				connectButton.Content = "Connected";

                nameLabel.IsEnabled = false;
                nameTextBox.IsEnabled = false;
                
				sendButton.IsEnabled = true;
                composeTextBox.IsEnabled = true;
				composeTextBox.Focus();
			}
			catch (Exception ex)
			{
				if (ex is SocketException || ex is FormatException)
				{
					MessageBox.Show(ex.ToString());
				}
				else
				{
					throw;
				}
			}
		}
        
        private void composeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                sendMessage();
            }
        }

        #endregion

        // called when a message is received
        public void OnBroadcast(object sender, NewBroadcastEventArgs e) =>
            Dispatcher.BeginInvoke(new Action(() =>
                Broadcast_NewBroadcast(sender, e)
            ));

        // which actually calls this
        private void Broadcast_NewBroadcast(object sender, NewBroadcastEventArgs e)
        {
            IPAddress remoteIP = IPAddress.Parse(e.message.Words);
            Client c = new Client(epLocal, remoteIP, port, e.message.Sender);

            switch (e.message.Event) // Add clients
            {
                case Event.Join: // Someone else joined
                case Event.Discovery: // Response to my join request (other people exist)
                    // check if client already exists before adding
                    if (!clients.Contains(c)) {
                        c.NewMessage += OnMessage;
                        c.Offline += OnClientOffline;
                        clients.Add(c);
                    }
                    break;
                case Event.Leave:
                    clients.Remove(c);
                    messages.Add(new LocalMessage($"{c.Name} went offline"));
                    break;
            }

            switch (e.message.Event) // idk idk idk (extra edge case stuff for event.join
            {
                case Event.Join:
                    RemoteMessage msg = new RemoteMessage(getLocalIP().MapToIPv4().ToString(), nameTextBox.Text);
                    msg.Event = Event.Discovery;
                    broadcast.Send(msg, port);
                    
                    messages.Add(new LocalMessage($"{c.Name} joined"));
                    break;
            }
        }

        private void OnClientOffline(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
                Client_Offline((Client)sender)
            ));
        }

        private void Client_Offline(Client sender)
        {
            clients.Remove(sender); // wack, someone went offline :/
            messages.Add(new LocalMessage($"{sender.Name} is offline"));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                RemoteMessage message = new RemoteMessage("", nameTextBox.Text); //we are the sender
                message.Event = Event.Leave;

                for (int i = 0; i < clients.Count; i++)
                {
                    clients[i].SendMessage(message);
                }
            }
            catch (SocketException ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // TODO: better offline messages and stuffs
        // also send message objects better
        // should self also be another client object?
    }
}

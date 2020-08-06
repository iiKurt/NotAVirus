using System;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using System.Net;
using System.Net.Sockets;

namespace NotAVirus
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		const ushort port = 3012;
		LocalClient self = new LocalClient(port);

		// has to be observable as WPF needs to know when it updates
		ObservableCollection<RemoteClient> clients = new ObservableCollection<RemoteClient>();
		ObservableCollection<Message> messages = new ObservableCollection<Message>();

		Broadcast broadcast;

		public MainWindow()
		{
			InitializeComponent();
			messagesListBox.ItemsSource = messages;
			clientsListBox.ItemsSource = clients;

			messages.CollectionChanged += Messages_CollectionChanged;
		}

		private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{ // scroll any new items into view
			messagesListBox.SelectedIndex = messagesListBox.Items.Count - 1;
			messagesListBox.ScrollIntoView(messagesListBox.SelectedItem);
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
					clients[i].Send(message);
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
		private void composeTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				sendMessage();
			}
		}

		private void connectButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				// broadcast that we are online
				broadcast = new Broadcast(self.IP, port);
				broadcast.NewBroadcast += OnBroadcast;

				RemoteMessage msg = new RemoteMessage(self.IP.MapToIPv4().ToString(), nameTextBox.Text);
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
			catch (SocketException ex)
			{
			    MessageBox.Show(ex.ToString());
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			try
			{
				RemoteMessage message = new RemoteMessage("", nameTextBox.Text); //we are the sender
				message.Event = Event.Leave;

				for (int i = 0; i < clients.Count; i++)
				{
					clients[i].Send(message);
				}
			}
			catch (SocketException ex)
			{
				MessageBox.Show(ex.ToString());
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
			RemoteClient c = new RemoteClient(self, remoteIP, port, e.message.Sender);

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
					RemoteMessage msg = new RemoteMessage(self.IP.MapToIPv4().ToString(), nameTextBox.Text);
					msg.Event = Event.Discovery;
					broadcast.Send(msg, port);
					
					messages.Add(new LocalMessage($"{c.Name} joined"));
					break;
			}
		}

		private void OnClientOffline(object sender, EventArgs e)
		{
			Dispatcher.BeginInvoke(new Action(() =>
				Client_Offline((RemoteClient)sender)
			));
		}

		private void Client_Offline(RemoteClient sender)
		{
			clients.Remove(sender); // wack, someone went offline :/
		}

		// TODO: better offline events and stuffs
		// Client should throw exception if can't send (replaces client offline event)
		// Client leave event
		// also send message objects better: void sendMessage(RemoteMessage message, Client destination) { ... }
	}
}

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

		private void sendTheMessage()
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
			try
			{
				// broadcast that we are online
				broadcast = new Broadcast(self.IP, port);
				broadcast.Join += OnJoin;
				broadcast.Discovery += OnDiscovery;

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
		
		private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{ // scroll any new items into view
			messagesListBox.SelectedIndex = messagesListBox.Items.Count - 1;
			messagesListBox.ScrollIntoView(messagesListBox.SelectedItem);
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

		#region Broadcast

		// called when someone joins the chat
		public void OnJoin(object sender, NewBroadcastEventArgs e) =>
			Dispatcher.BeginInvoke(new Action(() =>
				Broadcast_Join(sender, e)
			));

		// which actually calls this
		private void Broadcast_Join(object sender, NewBroadcastEventArgs e)
		{
			// when message class has sender as object this will be abstracted away
			IPAddress remoteIP = IPAddress.Parse(e.message.Words);
			RemoteClient c = new RemoteClient(self, remoteIP, port, e.message.Sender);

			// check if client already exists before adding
			if (!clients.Contains(c))
			{ // Add the client
				c.NewMessage += OnClientNewMessage;
				c.Leave += OnClientLeave;
				clients.Add(c);
			}

			RemoteMessage msg = new RemoteMessage(self.IP.MapToIPv4().ToString(), nameTextBox.Text);
			msg.Event = Event.Discovery;
			broadcast.Send(msg, port);

			messages.Add(new LocalMessage($"{c.Name} joined"));
		}
		
		// reponse to my join message (other people exist)
		public void OnDiscovery(object sender, NewBroadcastEventArgs e) =>
			Dispatcher.BeginInvoke(new Action(() =>
				Broadcast_Discovery(sender, e)
			));
		
		private void Broadcast_Discovery(object sender, NewBroadcastEventArgs e)
		{
			// when message class has sender as object this will be abstracted away
			IPAddress remoteIP = IPAddress.Parse(e.message.Words);
			RemoteClient c = new RemoteClient(self, remoteIP, port, e.message.Sender);

			// check if client already exists before adding
			if (!clients.Contains(c))
			{ // Add the client
				c.NewMessage += OnClientNewMessage;
				c.Leave += OnClientLeave;
				clients.Add(c);
			}
		}

		#endregion

		#region Client Events

		// called when a NORMAL message is received
		public void OnClientNewMessage(object sender, NewMessageEventArgs e) =>
			Dispatcher.BeginInvoke(new Action(() =>
				Client_NewMessage((RemoteClient)sender, e.message)
			));

		// which actually just calls this
		public void Client_NewMessage(RemoteClient from, RemoteMessage message)
		{
			messages.Add(message);
		}

		private void OnClientLeave(object sender, EventArgs e)
		{
			Dispatcher.BeginInvoke(new Action(() =>
				Client_Leave((RemoteClient)sender)
			));
		}

		private void Client_Leave(RemoteClient sender)
		{
			clients.Remove(sender); // wack, someone went offline :/
			messages.Add(new LocalMessage($"{sender.Name} went offline"));
		}

		#endregion

		// TODO: test if any of this actually works
		// abstract broadcast away enough so that Join and Discovery events could be moved into RemoteClient?!
		// check client MessageCallback switch statment for details
	}
}

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
				RemoteMessage message = new RemoteMessage(composeTextBox.Text); //we are the sender

				for (int i = 0; i < clients.Count; i++)
				{
					clients[i].Send(message);
				}

				LocalMessage lmessage = new LocalMessage(composeTextBox.Text, "You");
				messages.Add(lmessage);
				composeTextBox.Clear();
			}
			catch (SocketException ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void addClient(RemoteClient client) // could be merged into some list updated event?
		{
			// check if client already exists before adding
			if (!clients.Contains(client))
			{ // Add the client
				client.NewMessage += OnClientNewMessage;
				client.Leave += OnClientLeave;
				clients.Add(client);
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
			/*try
			{*/
				// broadcast that we are online
				broadcast = new Broadcast(self, port);
				broadcast.Join += OnJoin;
				broadcast.Discovery += OnDiscovery;

				RemoteMessage msg = new RemoteMessage(nameTextBox.Text);
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
			/*}
			catch (SocketException ex)
			{
			    MessageBox.Show(ex.ToString());
			}*/
		}
		
		private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{ // scroll any new items into view
			messagesListBox.SelectedIndex = messagesListBox.Items.Count - 1;
			messagesListBox.ScrollIntoView(messagesListBox.SelectedItem);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
            //MessageBox.Show("goodbye");
			try
			{
				RemoteMessage message = new RemoteMessage(""); //we are the sender
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
			try
			{
				addClient(e.message.Sender);

				RemoteMessage msg = new RemoteMessage(nameTextBox.Text);
				msg.Event = Event.Discovery;
				broadcast.Send(msg, port);

				messages.Add(new InternalMessage($"{e.message.Sender.Name} joined"));
			}
			catch (Exception ex) // Just for debugging
			{
				MessageBox.Show(ex.Message);
				throw; // for the debugger
			}
		}
		
		// reponse to my join message (other people exist)
		public void OnDiscovery(object sender, NewBroadcastEventArgs e) =>
			Dispatcher.BeginInvoke(new Action(() =>
				addClient(e.message.Sender)
			));

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
            sender.Close();
			clients.Remove(sender); // wack, someone went offline :/
			messages.Add(new InternalMessage($"{sender.Name} went offline"));
		}

		#endregion

		// TODO: test if any of this actually works <==============

		// abstract broadcast away enough so that Join and Discovery events could be moved into RemoteClient?!
		// check client MessageCallback switch statment for details

		// could Join and Discovery messages actually be a different class to RemoteMessage?
		// ^ that would actually be real good to fix the dodgyness of the broadcast assigning sender to broadcast messages
		// have a InfoMessage class where there is a string name, IP (not transmitted over network), and RSA keys too
		// ^ yes real good idea lets do it sometime
		// Leave messages could be broadcast only but ehh (doesn't really matter at all)

		// rundown:
		// broadcast fixing and whatnot
		// direct messages
		// encryption
		// if I have time and it's secure: private rooms
	}
}

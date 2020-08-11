using System;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

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
                broadcast.Send(composeTextBox.Text);

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
			/*try // commented out for testing
			{*/
				// broadcast that we are online
				
				broadcast = new Broadcast(self.EP.Address, port);
				broadcast.Message += OnMessage;
				
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

		// called when new broadcast arrives
		public void OnMessage(object sender, NewBroadcastEventArgs e) =>
			Dispatcher.BeginInvoke(new Action(() =>
                Broadcast_Message(sender, e)
			));

		// which actually calls this
		private void Broadcast_Message(object sender, NewBroadcastEventArgs e)
		{
			RemoteMessage msg = new RemoteMessage(e.message);
            messages.Add(msg);

			//messages.Add(new InternalMessage($"{e.message.Sender.Name} joined"));
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
	}
}

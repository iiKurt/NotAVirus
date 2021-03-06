﻿using System;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using System.Net.Sockets;
using System.Diagnostics;

namespace NotAVirus
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
        bool connected = false;
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
                RemoteMessage message = new RemoteMessage(composeTextBox.Text);
                broadcast.Send(message);

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
				client.Message += OnClientMessage;
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
			try // commented out for testing
			{
                // broadcast that we are online

                broadcast = new Broadcast(self.EP.Address, port);
                broadcast.Join += OnBroadcastJoin;
                broadcast.Discovery += OnBroadcastDiscovery;
				broadcast.Message += OnMessage;
                broadcast.Leave += Broadcast_Leave;

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

        private void clientsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // https://stackoverflow.com/a/4888542
            var client = ((FrameworkElement)e.OriginalSource).DataContext as RemoteClient;
            if (client != null)
            {
                RemoteMessage msg = new RemoteMessage("DM test");
                client.Send(msg);
                MessageBox.Show("Sending test message to " + client.Name);
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

        // called when someone joins the chat
        public void OnBroadcastJoin(object sender, NewBroadcastEventArgs e) =>
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
                broadcast.Send(msg);

                messages.Add(new InternalMessage($"{e.message.Sender.Name} joined"));
            }
            catch (Exception ex) // Just for debugging
            {
                MessageBox.Show(ex.Message);
                throw; // for the debugger
            }
        }

        // response to my join message (other people exist)
        public void OnBroadcastDiscovery(object sender, NewBroadcastEventArgs e) =>
            Dispatcher.BeginInvoke(new Action(() =>
                addClient(e.message.Sender)
            ));
        
        // called upon a client leaving
        private void Broadcast_Leave(object sender, NewBroadcastEventArgs e) =>
            Dispatcher.BeginInvoke(new Action(() =>
                clients.Remove(e.message.Sender)
            ));

        // called when new broadcast arrives
        public void OnMessage(object sender, NewBroadcastEventArgs e) =>
			Dispatcher.BeginInvoke(new Action(() =>
                Broadcast_Message(sender, e)
			));

		// which actually calls this
		private void Broadcast_Message(object sender, NewBroadcastEventArgs e)
		{
            messages.Add(e.message);

			//messages.Add(new InternalMessage($"{e.message.Sender.Name} joined"));
		}

		#endregion

        // We don't touch this, okay?
		#region Client Events

		// called when a NORMAL message is received
		public void OnClientMessage(object sender, NewMessageEventArgs e) =>
			Dispatcher.BeginInvoke(new Action(() =>
				Client_Message((RemoteClient)sender, e.message)
			));

		// which actually just calls this
		public void Client_Message(RemoteClient from, RemoteMessage message)
		{
            //messages.Add(message);
            //MessageBox.Show(message.Contents);
            messages.Add(new InternalMessage(message.Contents));
            //Debug.WriteLine("New direct message from " + from.Name + "(Sender: " + message.Sender.Name + ") Contents: " + message.Contents);
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

        // TODO: how to handle calling events that are not assigned
    }
}

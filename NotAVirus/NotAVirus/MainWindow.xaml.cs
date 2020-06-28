using System;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using System.Net;
using System.Net.Sockets;

namespace NotAVirus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		EndPoint epLocal;

		// has to be observable as WPF needs to know when it updates
		ObservableCollection<Client> clients = new ObservableCollection<Client>();
		ObservableCollection<Message> messages = new ObservableCollection<Message>();

        public MainWindow()
        {
            InitializeComponent();

			messagesListBox.ItemsSource = messages;

			localIPTextBox.Text = getLocalIP();
        }

		// TODO: check how I did this on the old versions
		private string getLocalIP()
		{
			IPHostEntry host;
			host = Dns.GetHostEntry(Dns.GetHostName());

			foreach (IPAddress ip in host.AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					return ip.ToString();
				}
			}

			return "127.0.0.1";
			//return "10.0.2.15";
		}

		// called when a message is received
		public void OnMessage(object sender, NewMessageEventArgs e) =>
			// this is running in an async thread
			// so we need to invoke the UI
			// TODO: check how I did this on the old versions
			Dispatcher.BeginInvoke(new Action(() =>
				NewMessage((Client)sender, e.message)
			));
		
		// which actually just calls this
		public void NewMessage(Client from, Message message)
		{
			try
			{
				messages.Add(message);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		// send a message
		private void sendButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Chat message = new Chat("Other", composeTextBox.Text);

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

		private void connectButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				epLocal = new IPEndPoint(IPAddress.Parse(localIPTextBox.Text), Convert.ToInt32(localPortTextBox.Text));
				
				clients.Add(new Client(epLocal, IPAddress.Parse(remoteIPTextBox.Text), Convert.ToInt32(remotePortTextBox.Text)));

				for (int i = 0; i < clients.Count; i++)
				{
					clients[i].NewMessage += new EventHandler<NewMessageEventArgs>(OnMessage);
				}

				connectButton.IsEnabled = false;
				connectButton.Content = "Connected";

				sendButton.IsEnabled = true;
				composeTextBox.Focus();
			}
			catch (Exception ex)
			{
				if (ex is SocketException || ex is FormatException)
				{
					MessageBox.Show(ex.ToString());
				}
				throw;
			}
		}
	}
}

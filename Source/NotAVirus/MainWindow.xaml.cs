using System;
using System.Windows;
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

            broadcast = new Broadcast(getLocalIP(), port);
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
				NewMessage((Client)sender, e.message)
			));
		
		// which actually just calls this
		public void NewMessage(Client from, RemoteMessage message)
		{
			try
			{
				switch (message.Event)
				{
					case Event.Message:
						messages.Add(message);
						break;
					default:
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
				RemoteMessage message = new RemoteMessage(composeTextBox.Text);

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

				//clients.Add(new Client(epLocal, IPAddress.Parse(remoteIPTextBox.Text), Convert.ToInt32(remotePortTextBox.Text)));

				// broadcast that we are online

				broadcast = new Broadcast(getLocalIP(), port);

				// we are 'connected'

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
				else
				{
					throw;
				}
			}
		}
	}
}

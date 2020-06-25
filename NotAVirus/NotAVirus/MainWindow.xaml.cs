using System;
using System.Text;
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
		Socket sock; //hehe sock
		EndPoint epLocal, epRemote;

		// has to be observable as WPF needs to know when it updates
		ObservableCollection<Message> messages = new ObservableCollection<Message>();

        public MainWindow()
        {
            InitializeComponent();

			messagesListBox.ItemsSource = messages;

			sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

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
		private void messageCallBack(IAsyncResult result)
		{
			try
			{
				int size = sock.EndReceiveFrom(result, ref epRemote);

				if (size > 0)
				{
					byte[] receivedData = new byte[1464];

					receivedData = (byte[])result.AsyncState;

					Chat message = new Chat(receivedData);

					// this is running in an async thread
					// so we need to invoke the UI
					// TODO: check how I did this on the old versions
					Dispatcher.BeginInvoke(new Action(() =>
						messages.Add(message)
					));
				}

				byte[] buffer = new byte[1500];
				sock.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epRemote, new AsyncCallback(messageCallBack), buffer);
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
				
				byte[] msg = message.Serialize();

				sock.Send(msg);

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
				sock.Bind(epLocal);

				epRemote = new IPEndPoint(IPAddress.Parse(remoteIPTextBox.Text), Convert.ToInt32(remotePortTextBox.Text));
				sock.Connect(epRemote);

				byte[] buffer = new byte[1500];
				sock.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epRemote, new AsyncCallback(messageCallBack), buffer);

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

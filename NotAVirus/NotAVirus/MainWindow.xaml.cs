using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        public MainWindow()
        {
            InitializeComponent();

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

		private void messageCallBack(IAsyncResult result)
		{
			try
			{
				int size = sock.EndReceiveFrom(result, ref epRemote);

				if (size > 0)
				{
					byte[] receivedData = new byte[1464];

					receivedData = (byte[])result.AsyncState;

					ASCIIEncoding encoder = new ASCIIEncoding();
					string receivedMessage = encoder.GetString(receivedData);

					// this is running in an async thread
					// so we need to invoke the UI
					// TODO: check how I did this on the old versions
					Dispatcher.BeginInvoke(new Action(() =>
						messagesListBox.Items.Add("Other: " + receivedMessage)
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

		private void sendButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ASCIIEncoding encoder = new ASCIIEncoding();
				byte[] msg = new byte[1500];
				msg = encoder.GetBytes(composeTextBox.Text);

				sock.Send(msg);
				messagesListBox.Items.Add("You: " + composeTextBox.Text);
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

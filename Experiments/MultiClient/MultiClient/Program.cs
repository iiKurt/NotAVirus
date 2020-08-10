using System;
using System.Collections.Generic;
using System.Net;

namespace MultiClient
{
	class Program
	{
		const ushort port = 3012;

		static LocalClient self = new LocalClient(port);
		static List<RemoteClient> clients = new List<RemoteClient>();
        static Broadcast broadcast = new Broadcast();

		static void Main(string[] args)
		{
			Console.WriteLine("Your IP: " + self.EP.ToString());
			Console.WriteLine();

			Console.WriteLine("Enter 1st client IP: ");
			IPAddress ip1 = IPAddress.Parse(Console.ReadLine());
			clients.Add(new RemoteClient(self, ip1, port, "3st"));

			Console.WriteLine("Enter 2nd client IP: ");
			IPAddress ip2 = IPAddress.Parse(Console.ReadLine());
			clients.Add(new RemoteClient(self, ip2, port, "2nd"));

			Console.WriteLine("Listening for messages from those clients...");
			foreach (RemoteClient client in clients)
			{
				client.NewMessage += Client_NewMessage;
			}

			while (true)
			{
				Console.WriteLine("Enter a message to send to both of those clients: ");
				string message = Console.ReadLine();
				foreach (RemoteClient client in clients)
				{
					client.Send(message);
				}
			}
		}

		private static void Client_NewMessage(object sender, NewMessageEventArgs e)
		{
			Console.WriteLine(e.message);
		}
	}
}

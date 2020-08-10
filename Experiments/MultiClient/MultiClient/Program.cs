using System;

namespace MultiClient
{
	class Program
	{
		const ushort port = 3012;

		static LocalClient self = new LocalClient(port);
        static Broadcast broadcast;

		static void Main(string[] args)
		{
			Console.WriteLine("Your IP: " + self.EP.ToString());
			Console.WriteLine();
            broadcast = new Broadcast(self.EP.Address);
            broadcast.Message += Broadcast_Message;

			Console.WriteLine("Listening for messages...");

			while (true)
			{
				Console.WriteLine("Enter a message to send: ");
				string message = Console.ReadLine();
                broadcast.Send(message);
			}
		}

        private static void Broadcast_Message(object sender, NewBroadcastEventArgs e)
        {
            Console.WriteLine("New Message: " + e.message);
        }

        private static void Client_NewMessage(object sender, NewMessageEventArgs e)
		{
			Console.WriteLine(e.message);
		}
	}
}

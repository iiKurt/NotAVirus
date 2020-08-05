using System.IO;

namespace NotAVirus
{
	public enum Sign
	{
		Unsigned, Admin
	}

	public enum Event
	{
		Join, // When someone joins
		Discovery, // Response to when someone joins, to determine who's online
		Message, // A normal message
		Leave // When someone leaves
	}

	public abstract class Message
	{
		public string Contents { get; set; }
	}

	public class LocalMessage : Message
	{

	}

	public class RemoteMessage : Message
	{
		public int Version = 0;
		public bool Direct = false;
		public Sign Signed = Sign.Unsigned;
		public Event Event = Event.Message;
        
        // somehow use client instead of string on Sender

		// wpf only likes properties and not fields
		public string Sender { get; set; } // should be client object (with ips..?)
		public string Words { get; set; }
		// override the string in the MessageItem class
		public new string Contents { get
			{
				return Sender + ": " + Words;
			}
		}

		public RemoteMessage(string Words, string Sender = "Other")
		{
			this.Event = Event.Message;
			this.Sender = Sender;
			this.Words = Words;
		}

		public RemoteMessage(Event Event)
		{
			this.Event = Event;
			if (Event == Event.Message)
			{
				this.Sender = "";
				this.Words = "";
			}
		}

		// https://stackoverflow.com/a/1446612
		public byte[] Serialize()
		{
			using (MemoryStream m = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(m))
				{
					writer.Write(Version);
					writer.Write((int)Event);
					if (Event == Event.Message)
					{
						writer.Write(Sender);
						writer.Write(Words);
					}
				}
				return m.ToArray();
			}
		}

		public RemoteMessage(byte[] data) //Deserialise
		{
			using (MemoryStream m = new MemoryStream(data))
			{
				using (BinaryReader reader = new BinaryReader(m))
				{ // order of these statements matter
					this.Version = reader.ReadInt32();
					this.Event = (Event)reader.ReadInt32();
					if (Event == Event.Message)
					{
						this.Sender = reader.ReadString();
						this.Words = reader.ReadString();
					}
				}
			}
		}
	}
}

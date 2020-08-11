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

	public class InternalMessage : Message
	{
		public InternalMessage(string contents)
		{
			Contents = contents;
		}
	}

	public class LocalMessage : Message
	{
		public new string Contents
		{
			get
			{
				return Sender + ": " + Words;
			}
		}
		public string Sender { get; set; }
		public string Words { get; set; }

		public LocalMessage(string words, string sender)
        {
			Words = words;
            Sender = sender;
        }
	}

	public class RemoteMessage : Message
	{
		public int Version = 0;
		public bool Direct = false;
		public Sign Signed = Sign.Unsigned;
		public Event Event = Event.Message;
        
		// wpf only likes properties and not fields
		public RemoteClient Sender { get; set; } // never transmitted over network, just inferred from incoming IP
		public string Words { get; set; }
		// override the string in the MessageItem class
		public new string Contents { get
			{
				return "Someone: " + Words;
			}
		}

		public RemoteMessage(string words)
		{
			this.Event = Event.Message;
            this.Words = words;
		}

		public RemoteMessage(Event Event)
		{
			this.Event = Event;
			if (Event == Event.Message)
			{
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
					writer.Write(Words);
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
					this.Words = reader.ReadString();
				}
			}
		}
	}
}

using System.IO;

namespace NotAVirus
{
	public enum Sign
	{
		Unsigned, Admin
	}

	public abstract class Message
	{
	}

	public class LocalMessage : Message
	{

	}

	public class RemoteMessage : Message
	{
		public int Version = 0;
		public bool Direct = false;
		public Sign Signed = Sign.Unsigned;
        
        // somehow use client instead of string on Sender

		// wpf liks properties and not fields
		public string Sender { get; set; }
		public string Words { get; set; }
		// override the string in the MessageItem class
		public string Contents { get
			{
				return Sender + ": " + Words;
			}
		}

		public RemoteMessage(string Sender, string Words)
		{
			this.Sender = Sender;
			this.Words = Words;
		}

		// https://stackoverflow.com/a/1446612
		public byte[] Serialize()
		{
			using (MemoryStream m = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(m))
				{
					writer.Write(Version);
					writer.Write(Sender);
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
					this.Sender = reader.ReadString();
					this.Words = reader.ReadString();
				}
			}
		}
	}
}

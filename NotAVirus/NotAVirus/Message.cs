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

		public virtual byte[] Serialize()
		{
			return null;
		}
		public RemoteMessage(byte[] data)
		{

		}
	}

	public class Event : RemoteMessage
	{

	}

	public class Chat : RemoteMessage
	{
		// wpf liks properties and not fields
		public string Sender { get; set; }
		public string Words { get; set; }
		// override the string in the MessageItem class
		public string Contents { get
			{
				return Sender + ": " + Words;
			}
		}

		public Chat(string Sender, string Words)
		{
			this.Sender = Sender;
			this.Words = Words;
		}

		// https://stackoverflow.com/a/1446612
		public override byte[] Serialize()
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

		public Chat(byte[] data) //Deserialise
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

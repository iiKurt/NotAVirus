using System.IO;

namespace NotAVirus
{
	class Message
	{
		public string Contents { get; set; }
	}

	class Chat : Message
	{
		public int Version;
		// wpf liks properties and not fields
		public string Sender { get; set; }
		public string Words { get; set; }
		// override the string in the MessageItem class
		public new string Contents { get
			{
				return Sender + ": " + Words;
			}
		}

		public Chat(string Sender, string Contents)
		{
			this.Version = 0;
			this.Sender = Sender;
			this.Words = Contents;
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

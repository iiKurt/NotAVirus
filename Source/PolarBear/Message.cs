using System.IO;
using System.Windows.Media;

namespace PolarBear
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
        public Brush Color { get; set; } = new SolidColorBrush(Colors.Black);
    }

    public class InternalMessage : Message
    {
        public InternalMessage(string contents, Color? color = null)
        {
            Contents = contents;
            if (color == null)
            {
                Color = new SolidColorBrush(Colors.Black);
            }
            else
            {
                Color = new SolidColorBrush((Color)color);
            }
        }
    }

    public class LocalMessage : Message
    {
        public LocalMessage(string contents)
        {
            Contents = contents;
            Color = new SolidColorBrush(Colors.MediumBlue);
        }
    }

    public class RemoteMessage : Message
    {
        public int Version = 0;
        public bool Direct = false;
        public Sign Signed = Sign.Unsigned;
        public Event Event = Event.Message;

        public RemoteMessage(string contents, Event msgEvent = Event.Message)
        {
            this.Event = msgEvent;
            this.Contents = contents;
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
                    writer.Write(Contents);
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
                    this.Contents = reader.ReadString();
                }
            }
        }
    }
}

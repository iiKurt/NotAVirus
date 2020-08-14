namespace PolarBear
{
    public abstract class Client
    {
        public string Name { get; set; }
    }

    public class LocalClient : Client { }

    public class RemoteClient : Client
    {
        public RemoteClient(string name)
        {
            Name = name;
        }
    }
}

namespace SlackBotSender.Exceptions
{
    public class ClientAlreadyInitializedException : Exception
    {
        public ClientAlreadyInitializedException() : base("Client already initialized") { }
    }

    public class ClientNotInitializedException : Exception
    {
        public ClientNotInitializedException() : base("Client not yet initialized") { }
    }

    public class MessageNotSpecifiedException : Exception
    {
        public MessageNotSpecifiedException() : base("Message not specified") { }
    }

    public class ChannelNotSpecifiedException : Exception
    {
        public ChannelNotSpecifiedException() : base("Channel not specified") { }
    }

    public class InvalidAuthennticationException : Exception
    {
        public InvalidAuthennticationException() : base("Channel not specified") { }
    }

    public class ChannelNotFoundException : Exception
    {
        public string ChannelId { get; }
        public ChannelNotFoundException(string channelId) : base(string.Format("Channel {0} not found", channelId)) => ChannelId = channelId;
    }

    public class RegisterMessageInDbException : Exception
    {
        public RegisterMessageInDbException(Exception? ex) : base("Помилка при занесенні до бази даних", ex) { }
    }
}

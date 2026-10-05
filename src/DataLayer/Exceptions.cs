namespace DataLayer.Exceptions
{
    public class BundleAlreadyExistsException : Exception
    {
        public BundleAlreadyExistsException() : base("Bundle already exists") { }
    }

    public class ChannelAlreadyExistsException : Exception
    {
        public ChannelAlreadyExistsException() : base("Channel already exists") { }
    }
}

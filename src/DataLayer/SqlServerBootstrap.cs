using System.Data;
using Dapper;
using DataLayer.Exceptions;
using DataLayer.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace DataLayer;

public static class SqlServerBootstrap
{
    private static bool UseMockingData { get; }

    static SqlServerBootstrap()
    {
#if DEBUG
        UseMockingData = true;
#endif
    }

    public static async Task EnsureDbAsync(string serverConnectionString)
    {
        var DatabaseName = "CrybbBot";
        var connectionString = serverConnectionString;

        var split = connectionString.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        var dbId = split.FirstOrDefault(p => p.StartsWith("database", StringComparison.InvariantCultureIgnoreCase));
        if (dbId != null)
        {
            split.Remove(dbId);
            var second = dbId.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (!string.IsNullOrWhiteSpace(second))
            {
                DatabaseName = second;
            }
            connectionString = string.Join(";", split);
        }

        // 1) Create DB if missing
        var dbNameEscaped = DatabaseName.Replace("'", "''");
        var createDbSql = string.Format(SQL.DB_MSSQL.CreateDb, DatabaseName);

        await using (var master = new SqlConnection(connectionString))
        {
            await master.OpenAsync();
            await master.ExecuteAsync(createDbSql);
        }

        //var split = connectionString.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        //split.Add($"Database={DatabaseName}");
        //connectionString = string.Join(";", split);

        // 2) Connect to the created DB and apply schema
        connectionString = serverConnectionString;

        var csb = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = DatabaseName
        };

        await using var conn = new SqlConnection(csb.ToString());
        await conn.OpenAsync();

        var schemaSql = SQL.DB_MSSQL.CreateTables;

        await conn.ExecuteAsync(schemaSql, commandType: CommandType.Text);
    }

    public static async Task<List<Message>> GetAllMessages(DateTime from)
    {
        if (UseMockingData)
        {
            var messages = MockingData.Messages;
            //foreach (var message in messages)
            //{
            //}
            return messages;
        }
        return new List<Message>();
    }
    
    public static async Task<List<Bundle>> GetAllBundles(bool includeEmpty = false)
    {
        if (UseMockingData)
        {
            List<Bundle>? bundles = null;
            //if (includeEmpty)
            //{
                bundles = MockingData.Bundles;
            //}
            //else
            //{
            //    bundles = MockingData.Bundles.Where(p => MockingData.BundleChannelMappings.Any(q => q.BundleID.Equals(p.ID))).ToList();
            //}
            if (bundles.Any())
            {
                foreach (var bundle in bundles)
                {
                    bundle.BundleChannelMappings = MockingData.BundleChannelMappings.Where(p => p.BundleID.Equals(bundle.ID)).ToList();
                    //if (bundle.BundleChannelMappings.Any())
                    //{
                    //    foreach (var mapping in bundle.BundleChannelMappings)
                    //    {
                    //        mapping.Channel = MockingData.Channels.FirstOrDefault(p => p.ID.Equals(mapping.ChannelID));
                    //    }
                    //}
                }
            }
            return bundles;
        }
        return new List<Bundle>();
    }

    public static async Task<Bundle?> GetBundle(Guid id)
    {
        if (UseMockingData)
        {
            var bundle = MockingData.Bundles.FirstOrDefault(p => p.ID.Equals(id));
            if (bundle != null)
            {
                bundle.BundleChannelMappings = MockingData.BundleChannelMappings.Where(p => p.BundleID.Equals(bundle.ID)).ToList();
            }
            return bundle;
        }
        return new Bundle();
    }

    public static async Task<List<Channel>> GetAllChannels(Guid? bundleId)
    {
        if (UseMockingData)
        {
            if (!bundleId.HasValue)
            {
                return MockingData.Channels;
            }
            else
            {
                return MockingData.Channels.Where(p => !MockingData.BundleChannelMappings.Any(q => q.BundleID.Equals(bundleId.Value) && q.ChannelID.Equals(p.ID))).ToList();
            }
        }
        return new List<Channel>();
    }

    public static async Task<Channel?> GetChannel(Guid channelId)
    {
        if (UseMockingData)
        {
            return MockingData.Channels.FirstOrDefault(p => p.ID.Equals(channelId));
        }
        return new Channel();
    }

    public static async Task<Channel?> GetChannel(string channelId)
    {
        if (UseMockingData)
        {
            return MockingData.Channels.FirstOrDefault(p => p.SlackChannelID.Equals(channelId));
        }
        return new Channel();
    }

    public static async Task<List<AttachedAttachment>> GetAttachments()
    {
        if (UseMockingData)
        {
            return MockingData.Attachments;
        }
        return new List<AttachedAttachment>();
    }

    public static async Task AddBundle(Bundle bundle)
    {
        if (UseMockingData)
        {
            if (MockingData.Bundles.Where(p => !string.IsNullOrWhiteSpace(p.Alias)).Any(p => p.ID.Equals(bundle.ID) || p.Alias!.Equals(bundle.Alias)))
            {
                throw new BundleAlreadyExistsException();
            }
            MockingData.Bundles.Add(bundle);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static async Task DeleteBundle(Guid id)
    {
        if (UseMockingData)
        {
            var bundle = MockingData.Bundles.FirstOrDefault(p => p.ID.Equals(id));
            if (bundle != null)
            {
                MockingData.Bundles.Remove(bundle);
            }
            var mappings = MockingData.BundleChannelMappings.Where(p => p.BundleID.Equals(id)).ToList();
            if (mappings.Any())
            {
                foreach (var mapping in mappings)
                {
                    var channel = mapping.Channel ?? await GetChannel(mapping.ChannelID);
                    if (channel?.BundleChannelMappings != null)
                    {
                        channel.BundleChannelMappings.Remove(mapping);
                    }
                    MockingData.BundleChannelMappings.Remove(mapping);
                }
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static async Task AddChannel(Channel channel)
    {
        if (UseMockingData)
        {
            if (MockingData.Channels.Any(p => p.ID.Equals(channel.ID) || (!string.IsNullOrWhiteSpace(p.Alias) &&
                                                                          !string.IsNullOrWhiteSpace(channel.Alias) &&
                                                                          p.Alias.Equals(channel.Alias))))
            {
                throw new ChannelAlreadyExistsException();
            }
            MockingData.Channels.Add(channel);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static async Task AddBundleChannelMapping(BundleChannelMapping mapping)
    {
        if (UseMockingData)
        {
            if (MockingData.BundleChannelMappings.Any(p => p.ChannelID.Equals(mapping.ChannelID) && p.BundleID.Equals(mapping.BundleID)))
            {
                return;
            }
            MockingData.BundleChannelMappings.Add(mapping);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static async Task DeleteChannel(Guid id, Guid? bundleId)
    {
        if (UseMockingData)
        {
            var channel = MockingData.Channels.FirstOrDefault(p => p.ID.Equals(id));
            
            // delete from all
            if (!bundleId.HasValue)
            {
                if (channel != null)
                {
                    MockingData.Channels.Remove(channel);
                }
            }

            var mappings = bundleId.HasValue
                ? MockingData.BundleChannelMappings.Where(p => p.ChannelID.Equals(id) && p.BundleID.Equals(bundleId.Value)).ToList()
                : MockingData.BundleChannelMappings.Where(p => p.ChannelID.Equals(id)).ToList();
            if (mappings.Any())
            {
                foreach (var mapping in mappings)
                {
                    MockingData.BundleChannelMappings.Remove(mapping);

                    var bundle = mapping.Bundle;
                    if (bundle == null && bundleId.HasValue)
                    {
                        bundle = MockingData.Bundles.FirstOrDefault(p => p.ID.Equals(bundleId.Value));
                    }
                    if (bundle?.BundleChannelMappings != null)
                    {
                        bundle.BundleChannelMappings.Remove(mapping);
                    }

                    if (channel?.BundleChannelMappings != null)
                    {
                        channel.BundleChannelMappings.Remove(mapping);
                    }
                }
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static async Task AddAttachedBundle(AttachedBundle bundle)
    {
        if (UseMockingData)
        {
            MockingData.AttachedBundles.Add(bundle);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static async Task AddAttachedChannel(AttachedChannel channel)
    {
        if (UseMockingData)
        {
            MockingData.AttachedChannels.Add(channel);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static async Task AddMessage(Message message)
    {
        if (UseMockingData)
        {
            MockingData.Messages.Add(message);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static async Task AddSendingError(SendingError error)
    {
        if (UseMockingData)
        {
            MockingData.SendingErrors.Add(error);
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}

public static class MockingData
{
    public sealed class SavedMockingData
    {
        public List<BundleChannelMapping>? BundleChannelMappings { get; set; }
        public List<Bundle>? Bundles { get; set; }
        public List<Channel>? Channels { get; set; }
        public List<Message>? Messages { get; set; }
        public List<AttachedBundle>? AttachedBundles { get; set; }
        public List<AttachedChannel>? AttachedChannels { get; set; }
        public List<AttachedAttachment>? Attachments { get; set; }
        public List<SendingError>? SendingErrors { get; set; }
    }

    internal static List<BundleChannelMapping> BundleChannelMappings { get; }
    internal static List<Bundle> Bundles { get; }
    internal static List<Channel> Channels { get; }
    internal static List<Message> Messages { get; }
    internal static List<SendingError> SendingErrors { get; }
    internal static List<AttachedAttachment> Attachments { get; }
    internal static List<AttachedBundle> AttachedBundles { get; }
    internal static List<AttachedChannel> AttachedChannels { get; }

    static MockingData()
    {
        //var cIds = new[]
        //{
        //    Guid.NewGuid(),
        //    Guid.NewGuid()
        //};
        //Channels = new List<Channel>
        //{
        //    new Channel
        //    {
        //        ID = cIds[0],
        //        Alias = "Тестовий канал",
        //        SlackChannelID = "D0AC9MA4HFZ"
        //    },
        //    new Channel
        //    {
        //        ID = cIds[1],
        //        Alias = "Фейковий канал",
        //        SlackChannelID = "00000000000"
        //    }
        //};

        Channels = new List<Channel>();

        //var bIds = new[]
        //{
        //    Guid.NewGuid()
        //};
        Bundles = new List<Bundle>();
        //{
        //    new Bundle
        //    {
        //        ID = bIds[0],
        //        Alias = "Тестова група"
        //    }
        //};

        BundleChannelMappings = new List<BundleChannelMapping>();

        Messages = new List<Message>();

        SendingErrors = new List<SendingError>();

        Attachments = new List<AttachedAttachment>();

        AttachedBundles = new List<AttachedBundle>();

        AttachedChannels = new List<AttachedChannel>();

        //var bmIds = new[]
        //{
        //    Guid.NewGuid()
        //};
        //BundleChannelMappings = new List<BundleChannelMapping>
        //{
        //    new BundleChannelMapping
        //    {
        //        BundleID = Bundles[0].ID,
        //        Bundle = Bundles[0],
        //        ChannelID = Channels[0].ID,
        //        Channel = Channels[0]
        //    }
        //};

        //Bundles[0].BundleChannelMappings = new List<BundleChannelMapping>
        //{
        //    BundleChannelMappings[0]
        //};

        //Channels[0].BundleChannelMappings = new List<BundleChannelMapping>
        //{
        //    BundleChannelMappings[0]
        //};

        //var mIds = new[]
        //{
        //    Guid.NewGuid(),
        //    Guid.NewGuid()
        //};
        //var aIds = new[]
        //{
        //    Guid.NewGuid()
        //};
        //Messages = new List<Message>
        //{
        //    new Message
        //    {
        //        ID = mIds[0],
        //        Text = "Перше повідомлення",
        //        InsertDT = DateTime.Today.AddHours(9).AddMinutes(23),
        //        Channels = new List<AttachedChannel>
        //        {
        //            new AttachedChannel
        //            {
        //                ID = Guid.NewGuid(),
        //                MessageID = mIds[0],
        //                ChannelID = Channels[0].ID,
        //                Channel = Channels[0]
        //            }
        //        },
        //    },
        //    new Message
        //    {
        //        ID = mIds[1],
        //        Text = "Друне повідомлення",
        //        InsertDT = DateTime.Today.AddHours(9).AddMinutes(24),
        //        Channels = new List<AttachedChannel>
        //        {
        //            new AttachedChannel
        //            {
        //                ID = Guid.NewGuid(),
        //                MessageID = mIds[1],
        //                ChannelID = Channels[0].ID,
        //                Channel = Channels[0]
        //            }
        //        },
        //        Attachments = new List<AttachedAttachment>
        //        {
        //            new AttachedAttachment
        //            {
        //                ID = aIds[0],
        //                MessageID = mIds[1],
        //                Type = Enums.AttachmentType.Image,
        //                Path = @"C:\Users\PC\Pictures\test.png",
        //                Title = "Test image",
        //                ContentBin = File.ReadAllBytes(@"C:\Users\PC\Pictures\test.png")
        //            }
        //        }
        //    }
        //};
    }

    public static string GetSerialized()
    {
        var save = new SavedMockingData
        {
            BundleChannelMappings = BundleChannelMappings,
            Bundles = Bundles,
            Channels = Channels,
            Messages = Messages,
            Attachments = Attachments,
            AttachedBundles = AttachedBundles,
            AttachedChannels = AttachedChannels,
            SendingErrors = SendingErrors
        };
        return JsonConvert.SerializeObject(save, Formatting.Indented);
    }

    public static void Load(SavedMockingData save)
    {
        BundleChannelMappings.Clear();
        if (save.BundleChannelMappings?.Any() ?? false)
        {
            BundleChannelMappings.AddRange(save.BundleChannelMappings);
        }

        Bundles.Clear();
        if (save.Bundles?.Any() ?? false)
        {
            Bundles.AddRange(save.Bundles);
        }

        Channels.Clear();
        if (save.Channels?.Any() ?? false)
        {
            Channels.AddRange(save.Channels);
        }

        Messages.Clear();
        if (save.Messages?.Any() ?? false)
        {
            Messages.AddRange(save.Messages);
        }

        Attachments.Clear();
        if (save.Attachments?.Any() ?? false)
        {
            Attachments.AddRange(save.Attachments);
        }

        AttachedBundles.Clear();
        if (save.AttachedBundles?.Any() ?? false)
        {
            AttachedBundles.AddRange(save.AttachedBundles);
        }

        AttachedChannels.Clear();
        if (save.AttachedChannels?.Any() ?? false)
        {
            AttachedChannels.AddRange(save.AttachedChannels);
        }

        SendingErrors.Clear();
        if (save.SendingErrors?.Any() ?? false)
        {
            SendingErrors.AddRange(save.SendingErrors);
        }

        foreach (var mapping in BundleChannelMappings)
        {
            var bundle = Bundles.FirstOrDefault(p => p.ID.Equals(mapping.BundleID));
            if (bundle != null)
            {
                if (bundle.BundleChannelMappings == null)
                {
                    bundle.BundleChannelMappings = new List<BundleChannelMapping>();
                }
                bundle.BundleChannelMappings.Add(mapping);
                mapping.Bundle = bundle;
            }

            var channel = Channels.FirstOrDefault(p => p.ID.Equals(mapping.ChannelID));
            if (channel != null)
            {
                if (channel.BundleChannelMappings == null)
                {
                    channel.BundleChannelMappings = new List<BundleChannelMapping>();
                }
                channel.BundleChannelMappings.Add(mapping);
                mapping.Channel = channel;
            }
        }

        foreach (var attachedBundle in AttachedBundles)
        {
            var bundle = Bundles.FirstOrDefault(p => p.ID.Equals(attachedBundle.BundleID));
            attachedBundle.Bundle = bundle;

            var message = Messages.FirstOrDefault(p => p.ID.Equals(attachedBundle.MessageID));
            attachedBundle.Message = message;
            if (message != null)
            {
                if (message.Bundles == null)
                {
                    message.Bundles = new List<AttachedBundle>();
                }
                message.Bundles.Add(attachedBundle);
            }
        }

        foreach (var attachedChannel in AttachedChannels)
        {
            var channel = Channels.FirstOrDefault(p => p.ID.Equals(attachedChannel.ChannelID));
            attachedChannel.Channel = channel;

            var message = Messages.FirstOrDefault(p => p.ID.Equals(attachedChannel.MessageID));
            attachedChannel.Message = message;
            if (message != null)
            {
                if (message.Channels == null)
                {
                    message.Channels = new List<AttachedChannel>();
                }
                message.Channels.Add(attachedChannel);
            }
        }

        foreach (var error in SendingErrors)
        {
            var channel = Channels.FirstOrDefault(p => p.ID.Equals(error.ChannelID));
            error.Channel = channel;

            var message = Messages.FirstOrDefault(p => p.ID.Equals(error.MessageID));
            error.Message = message;
            if (message != null)
            {
                if (message.SendingErrors == null)
                {
                    message.SendingErrors = new List<SendingError>();
                }
                message.SendingErrors.Add(error);
            }
        }

        foreach (var attachment in Attachments)
        {
            var message = Messages.FirstOrDefault(p => p.ID.Equals(attachment.MessageID));
            attachment.Message = message;
            if (message != null)
            {
                if (message.Attachments == null)
                {
                    message.Attachments = new List<AttachedAttachment>();
                }
                message.Attachments.Add(attachment);
            }
        }
    }
}
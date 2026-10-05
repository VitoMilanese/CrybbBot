using System.Data;
using System.Globalization;
using Dapper;
using DataLayer.Interfaces;
using DataLayer.Models;

namespace DataLayer;

public sealed class Data
{
    private readonly IDbConnectionFactory _factory;

    static Data()
    {
        SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());
    }

    public Data() { }

    public Data(IDbConnectionFactory factory) => _factory = factory;

    #region SELECT
    /// <param name="selectionMask">0 - all; 1 - scheduled; 2 - sent; 3 - errors</param>
    /// <returns></returns>
    public async Task<List<Message>> GetAllMessages(MessagesFilter filter)
    {
        if (_factory == null) return new List<Message>();

        var order = filter.Ascending ? "ASC" : "DESC";

        var whereList = new List<string>();

        if (filter.CreatedFrom.HasValue)
        {
            whereList.Add("m.InsertDT >= @CreatedFromDT");
            if (filter.CreatedTo.HasValue)
            {
                whereList.Add("m.InsertDT <= @CreatedToDT");
            }
        }

        if (filter.ScheduledFrom.HasValue)
        {
            whereList.Add("m.ScheduleDT >= @ScheduledFromDT");
            if (filter.ScheduledTo.HasValue)
            {
                whereList.Add("m.ScheduleDT <= @ScheduledToDT");
            }
        }

        if (!filter.SelectAll)
        {
            if (filter.Scheduled.HasValue)
            {
                if (filter.Scheduled.Value)
                {
                    whereList.Add($"m.ScheduleDT IS NOT NULL");
                }
                else
                {
                    whereList.Add($"m.ScheduleDT IS NULL");
                }
            }

            if (filter.Sent.HasValue)
            {
                if (filter.Sent.Value)
                {
                    whereList.Add("m.SendingDT IS NOT NULL");
                    whereList.Add("m.Managed = 1");
                }
                else
                {
                    whereList.Add("m.SendingDT IS NULL");
                    whereList.Add("m.Managed = 0");
                }
            }

            if (filter.Errors.HasValue)
            {
                if (filter.Errors.Value)
                {
                    whereList.Add("EXISTS (SELECT 1 FROM SendingError e WHERE e.MessageID = m.ID)");
                }
                else
                {
                    whereList.Add("NOT EXISTS (SELECT 1 FROM SendingError e WHERE e.MessageID = m.ID)");
                }
            }

            if (filter.Attachments.HasValue)
            {
                if (filter.Attachments.Value)
                {
                    if (filter.AttType != MessagesFilter.AttachmentsType.None)
                    {
                        var ors = new List<string>();
                        var and = string.Empty;
                        if ((filter.AttType & MessagesFilter.AttachmentsType.Image) == MessagesFilter.AttachmentsType.Image)
                        {
                            ors.Add("a.Type = 1");
                        }
                        if ((filter.AttType & MessagesFilter.AttachmentsType.File) == MessagesFilter.AttachmentsType.File)
                        {
                            ors.Add("a.Type = 2");
                        }
                        if ((filter.AttType & MessagesFilter.AttachmentsType.FileReference) == MessagesFilter.AttachmentsType.FileReference)
                        {
                            ors.Add("a.Type = 3");
                        }
                        if (ors.Any())
                        {
                            and = $" AND ({string.Join(" OR ", ors)})";
                        }
                        whereList.Add($"EXISTS (SELECT 1 FROM AttachedAttachment a WHERE a.MessageID = m.ID{and})");
                    }
                }
                else
                {
                    whereList.Add("NOT EXISTS (SELECT 1 FROM AttachedAttachment a WHERE a.MessageID = m.ID)");
                }
            }

            if (filter.ContainingText && !string.IsNullOrWhiteSpace(filter.Text))
            {
                whereList.Add($"UPPER(m.Text) LIKE UPPER('%{filter.Text}%')");
            }
        }

        if (filter.SearchByChannels)
        {
            if (filter.Bundles != null && filter.Bundles.Any())
            {
                var ids = filter.Bundles.Select(p => $"'{p.ToString("D")}'");
                var strIds = string.Join(", ", ids);
                whereList.Add($"EXISTS (SELECT 1 FROM AttachedBundle b WHERE b.MessageID = m.ID AND b.BundleID IN ({strIds}))");
            }

            if (filter.Channels != null && filter.Channels.Any())
            {
                var ids = filter.Channels.Select(p => $"'{p.ToString("D")}'");
                var strIds = string.Join(", ", ids);
                whereList.Add($"EXISTS (SELECT 1 FROM AttachedChannel c WHERE c.MessageID = m.ID AND c.ChannelID IN ({strIds}))");
            }
        }

        var where = string.Join("\r\n  AND ", whereList);

        if (!string.IsNullOrWhiteSpace(where))
        {
            where = "WHERE " + where;
        }

        var sql = $@"
SELECT m.ID,
       m.Text,
       m.InsertDT,
       m.ScheduleDT,
       m.SendingDT,
       m.Managed
FROM Message m
{where}
ORDER BY InsertDT {order}
LIMIT 100;";

        using var conn = _factory.Create();

        var param = new
        {
            CreatedFromDT = filter.CreatedFrom.HasValue
                ? filter.CreatedFrom.Value.ToString("o")
                : null,
            CreatedToDT = filter.CreatedTo.HasValue
                ? filter.CreatedTo.Value.ToString("o")
                : null,
            ScheduledFromDT = filter.ScheduledFrom.HasValue
                ? filter.ScheduledFrom.Value.ToString("o")
                : null,
            ScheduledToDT = filter.ScheduledTo.HasValue
                ? filter.ScheduledTo.Value.ToString("o")
                : null,
        };

        var rows = await conn.QueryAsync<(string ID, string? Text, string InsertDT, string ScheduleDT, string SendingDT, int Managed)>(sql, param);

        var result = rows
            .Select(r => new Message
            {
                ID = Guid.Parse(r.ID),
                Text = r.Text ?? string.Empty,
                Managed = r.Managed != 0,
                InsertDT = string.IsNullOrWhiteSpace(r.InsertDT)
                    ? DateTime.MinValue
                    : DateTime.Parse(r.InsertDT, null, DateTimeStyles.RoundtripKind),
                ScheduleDT = string.IsNullOrWhiteSpace(r.ScheduleDT)
                    ? null
                    : DateTime.Parse(r.ScheduleDT, null, DateTimeStyles.RoundtripKind),
                SendingDT = string.IsNullOrWhiteSpace(r.SendingDT)
                    ? null
                    : DateTime.Parse(r.SendingDT, null, DateTimeStyles.RoundtripKind)
            })
            .ToList();

        return result;
    }

    public async Task<Message?> GetTopScheduledMessage()
    {
        if (_factory == null) return null;

        const string sql = @"
SELECT ID
FROM Message
WHERE Managed = 0 AND
      ScheduleDT IS NOT NULL AND
      ScheduleDT <= @Now
ORDER BY ScheduleDT ASC, InsertDT ASC
LIMIT 1;
";

        using var conn = _factory.Create();
        var rows = await conn.QueryAsync<string>(sql, new { Now = DateTime.Now.ToString("o") });

        if (!(rows?.Any(p => !string.IsNullOrWhiteSpace(p) && Guid.TryParse(p, out _)) ?? false))
        {
            return null;
        }

        var rowId = rows.FirstOrDefault();
        var id = Guid.Parse(rowId!);
        var message = await GetMessage(id);

        if (message?.Channels?.Any() ?? false)
        {
            foreach (var channel in message.Channels)
            {
                channel.Channel = await GetChannel(channel.ChannelID);
            }
        }

        if (message?.Bundles?.Any() ?? false)
        {
            foreach (var bundle in message.Bundles)
            {
                bundle.Bundle = await GetBundle(bundle.BundleID);
            }
        }

        return message;
    }

    public async Task<Message?> GetMessage(Guid id)
    {
        if (_factory == null) return null;

        const string sql = @"
SELECT
    m.ID,
    m.Text,
    m.InsertDT,
    m.ScheduleDT,
    m.SendingDT,
    m.Managed,

    a.ID,
    a.MessageID,
    a.Type,
    a.Path,
    a.Title,
    a.FieldID,
    a.ContentText,
    a.ContentBin,

    b.ID,
    b.MessageID,
    b.BundleID,

    c.ID,
    c.MessageID,
    c.ChannelID,

    e.ID,
    e.MessageID,
    e.ChannelID,
    e.Details,
    e.Type

FROM Message m
LEFT JOIN AttachedAttachment a ON a.MessageID = m.ID
LEFT JOIN AttachedBundle b ON b.MessageID = m.ID
LEFT JOIN AttachedChannel c ON c.MessageID = m.ID
LEFT JOIN SendingError e ON e.MessageID = m.ID
WHERE m.ID = @MessageID
";

        using var conn = _factory.Create();

        var messages = await conn.QueryAsync
                <Message,
                AttachedAttachment,
                AttachedBundle,
                AttachedChannel,
                SendingError,
                Message>
            (
                sql,
                (message, attachments, bundles, channel, error) =>
                {
                    if (attachments != null)
                    {
                        message.Attachments = new List<AttachedAttachment> { attachments };
                    }
                    if (bundles != null)
                    {
                        message.Bundles = new List<AttachedBundle> { bundles };
                    }
                    if (channel != null)
                    {
                        message.Channels = new List<AttachedChannel> { channel };
                    }
                    if (error != null)
                    {
                        message.SendingErrors = new List<SendingError> { error };
                    }
                    return message;
                },
                new { MessageID = id.ToString("D") },
                splitOn: "ID,ID,ID,ID"
            );

        if (!(messages?.Any() ?? false))
        {
            return null;
        }

        var first = messages.FirstOrDefault();
        var message = new Message
        {
            ID = first!.ID,
            Text= first.Text,
            InsertDT = first.InsertDT,
            ScheduleDT = first.ScheduleDT,
            SendingDT = first.SendingDT,
            Managed = first.Managed,
            Attachments = new List<AttachedAttachment>(),
            Bundles = new List<AttachedBundle>(),
            Channels = new List<AttachedChannel>(),
            SendingErrors = new List<SendingError>()
        };

        foreach (var msg in messages)
        {
            if (msg.Attachments?.Any() ?? false)
            {
                message.Attachments.AddRange(msg.Attachments);
            }
            if (msg.Bundles?.Any() ?? false)
            {
                message.Bundles.AddRange(msg.Bundles);
            }
            if (msg.Channels?.Any() ?? false)
            {
                message.Channels.AddRange(msg.Channels);
            }
            if (msg.SendingErrors?.Any() ?? false)
            {
                message.SendingErrors.AddRange(msg.SendingErrors);
            }
        }

        message.Attachments = message.Attachments?.DistinctBy(p => p.ID)?.ToList();
        message.Bundles = message.Bundles?.DistinctBy(p => p.ID)?.ToList();
        message.Channels = message.Channels?.DistinctBy(p => p.ID)?.ToList();
        message.SendingErrors = message.SendingErrors?.DistinctBy(p => p.ID)?.ToList();

        return message;
    }

    public async Task<List<AttachedBundle>> GetAttachedBundles(Guid messageId)
    {
        if (_factory == null) return new List<AttachedBundle>();

        const string sql = @"
SELECT
    a.ID,
    a.BundleID,

    b.Alias
FROM AttachedBundle a
JOIN Bundle b ON a.BundleID = b.ID
WHERE a.MessageID = @MessageID;";

        using var conn = _factory.Create();

        var result = await conn.QueryAsync
                <AttachedBundle,
                Bundle,
                AttachedBundle>
            (
                sql,
                (attachedBundle, bundle) =>
                {
                    bundle.ID = attachedBundle.BundleID;
                    attachedBundle.Bundle = bundle;
                    return attachedBundle;
                },
                new { MessageID = messageId.ToString("D") },
                splitOn: "ID,Alias"
            );

        return result?.ToList() ?? new List<AttachedBundle>();
    }

    public async Task<List<AttachedChannel>> GetAttachedChannels(Guid messageId)
    {
        if (_factory == null) return new List<AttachedChannel>();

        const string sql = @"
SELECT
    a.ID,
    a.ChannelID,

    c.Alias,
    c.SlackChannelID
FROM AttachedChannel a
JOIN Channel c ON a.ChannelID = c.ID
WHERE a.MessageID = @MessageID;";

        using var conn = _factory.Create();

        var result = await conn.QueryAsync
                <AttachedChannel,
                Channel,
                AttachedChannel>
            (
                sql,
                (attachedChannel, channel) =>
                {
                    channel.ID = attachedChannel.ChannelID;
                    attachedChannel.Channel = channel;
                    channel.AttachedChannels = new List<AttachedChannel> { attachedChannel };
                    return attachedChannel;
                },
                new { MessageID = messageId.ToString("D") },
                splitOn: "ID,Alias"
            );

        return result?.ToList() ?? new List<AttachedChannel>();
    }

    public async Task<List<Bundle>> GetAllBundles()
    {
        if (_factory == null) return new List<Bundle>();

        const string sql = @"SELECT ID, Alias FROM Bundle;";

        using var conn = _factory.Create();
        var rows = await conn.QueryAsync<(string ID, string Alias)>(sql);

        var result = rows
            .Select(row => new Bundle
            {
                ID = string.IsNullOrWhiteSpace(row.ID)
                    ? Guid.Empty
                    : Guid.Parse(row.ID),
                Alias = row.Alias
            })
            .ToList();

        return result;
    }

    public async Task<Bundle?> GetBundle(Guid id)
    {
        if (_factory == null) return null;

        const string sql = @"
SELECT
    b.ID            AS ID,
    b.Alias         AS Alias,

    c.ID            AS ID,
    c.SlackChannelID,
    c.Alias         AS Alias,

    m.ID            AS ID,
    m.BundleID      AS BundleID,
    m.ChannelID     AS ChannelID

FROM Bundle b
JOIN BundleChannelMapping m ON m.BundleID = b.ID
JOIN Channel c ON c.ID = m.ChannelID
WHERE b.ID = @BundleID
";

        using var conn = _factory.Create();

        var rows = await conn.QueryAsync
                <Bundle,
                Channel,
                BundleChannelMapping,
                Bundle>
            (
                sql,
                (bundle, channel, mapping) =>
                {
                    mapping.Channel = channel;
                    bundle.BundleChannelMappings = new List<BundleChannelMapping> { mapping };
                    return bundle;
                },
                new { BundleID = id.ToString("D") },
                splitOn: "ID,ID"
            );

        if (!(rows?.Any() ?? false))
        {
            return null;
        }

        var row = rows!.First();
        var bundle = new Bundle
        {
            ID = row.ID,
            Alias = row.Alias,
            BundleChannelMappings = rows!.Where(q => q.BundleChannelMappings != null).SelectMany(q => q.BundleChannelMappings!)?.ToList()
        };

        return bundle;
    }

    public async Task<List<Channel>> GetAllChannels(Guid? bundleId)
    {
        if (_factory == null) return new List<Channel>();

        const string sql1 = @"
SELECT
    m.ID            AS ID,
    m.BundleID      AS BundleID,
    m.ChannelID     AS ChannelID,

    b.ID            AS ID,
    b.Alias         AS Alias,

    c.ID            AS ID,
    c.SlackChannelID,
    c.Alias         AS Alias

FROM BundleChannelMapping m
JOIN Bundle b ON m.BundleID = b.ID
JOIN Channel c ON m.ChannelID = c.ID
WHERE m.BundleID = @BundleID;
";

        const string sql2 = @"
SELECT
    m.ID            AS ID,
    m.BundleID      AS BundleID,
    m.ChannelID     AS ChannelID,

    b.ID            AS ID,
    b.Alias         AS Alias,

    c.ID            AS ID,
    c.SlackChannelID,
    c.Alias         AS Alias

FROM BundleChannelMapping m
JOIN Bundle b ON m.BundleID = b.ID
JOIN Channel c ON m.ChannelID = c.ID
";

        const string sql3 = @"
SELECT ID, SlackChannelID, Alias
FROM Channel c
WHERE NOT EXISTS (
    SELECT 1
    FROM BundleChannelMapping m
    WHERE m.ChannelID = c.ID
);";

        using var conn = _factory.Create();

        IEnumerable<BundleChannelMapping>? mappings = null;
        List<Channel>? more = null;

        if (bundleId.HasValue)
        {
            mappings = await conn.QueryAsync
                <BundleChannelMapping,
                Bundle,
                Channel,
                BundleChannelMapping>
            (
                sql1,
                (mapping, bundle, channel) =>
                {
                    mapping.Bundle = bundle;
                    mapping.Channel = channel;
                    return mapping;
                },
                new { BundleID = bundleId.Value.ToString("D") },
                splitOn: "ID,ID"
            );
        }
        else
        {
            mappings = await conn.QueryAsync
                <BundleChannelMapping,
                Bundle,
                Channel,
                BundleChannelMapping>
            (
                sql2,
                (mapping, bundle, channel) =>
                {
                    mapping.Bundle = bundle;
                    mapping.Channel = channel;
                    return mapping;
                },
                splitOn: "ID,ID"
            );

            var rows = await conn.QueryAsync<(string ID, string SlackChannelID, string? Alias)>(sql3);
            more = rows
                .Select(row => new Channel
                {
                    ID = string.IsNullOrWhiteSpace(row.ID)
                        ? Guid.Empty
                        : Guid.Parse(row.ID),
                    SlackChannelID = row.SlackChannelID,
                    Alias = row.Alias
                })
                .ToList();
        }

        if (mappings == null || !mappings.Any())
        {
            return new List<Channel>();
        }

        var result = new List<Channel>();

        if (more != null && more.Any())
        {
            result.AddRange(more);
        }

        foreach (var mapping in mappings.Where(p => p.Channel != null))
        {
            if (!result.Any(p => p.ID.Equals(mapping.Channel!.ID)))
            {
                result.Add(mapping.Channel!);

                if (mapping.Channel!.BundleChannelMappings == null)
                {
                    mapping.Channel!.BundleChannelMappings = new List<BundleChannelMapping>();
                }
                mapping.Channel!.BundleChannelMappings.Add(mapping);

                if (mapping.Bundle!.BundleChannelMappings == null)
                {
                    mapping.Bundle!.BundleChannelMappings = new List<BundleChannelMapping>();
                }
                mapping.Bundle!.BundleChannelMappings.Add(mapping);
            }
        }

        return result;

        #region Garbage
        //if (mappings != null && mappings.Any())
        //{
        //    //var bunleIds = mappings.Select(p => p.bun)

        //    const string sql1 = @"SELECt ID, Alias FROM BUNDLE WHERE ID = @ID LIMIT 1;";
        //}

        /*const string sql0a = @"SELECT ID, BundleID, ChannelID FROM BundleChannelMapping WHERE BundleID = @BundleID";
        const string sql0b = @"SELECT ID, BundleID, ChannelID FROM BundleChannelMapping";
        
        using var conn = _factory.Create();
        var rows = bundleId.HasValue
            ? await conn.QueryAsync<(string ID, string BundleID, string ChannelID)>(sql0a, new { BundleID = bundleId.Value.ToString("D") })
            : await conn.QueryAsync<(string ID, string BundleID, string ChannelID)>(sql0b);

        var mappings = rows
            .Select(r => new BundleChannelMapping
            {
                ID = string.IsNullOrWhiteSpace(r.ID)
                    ? Guid.Empty
                    : Guid.Parse(r.ID),
                BundleID = string.IsNullOrWhiteSpace(r.BundleID)
                    ? Guid.Empty
                    : Guid.Parse(r.BundleID),
                ChannelID = string.IsNullOrWhiteSpace(r.ChannelID)
                    ? Guid.Empty
                    : Guid.Parse(r.ChannelID)
            })
            .ToList();

        const string sql1 = @"SELECT ID, Alias FROM Bundle WHERE ID IN";*/

        //const string sql = @"SELECT ID, SlackChannelID, Alias FROM Channel WHERE ID = @ID LIMIT 1;";

        //using var conn = _factory.Create();
        //var row = await conn.QuerySingleOrDefaultAsync<(string ID, string SlackChannelID, string Alias)>(sql, new { ID = id.ToString("D") });

        //if (row.ID is null)
        //    return null;

        //var result = new Bundle
        //{
        //    ID = Guid.Parse(row.ID),
        //    Alias = row.Alias
        //};

        //return result;
        return new List<Channel>();
        #endregion
    }

    public async Task<Channel?> GetChannel(Guid channelId)
    {
        if (_factory == null) return null;

        const string sql = @"SELECT ID, SlackChannelID, Alias FROM Channel WHERE ID = @ID LIMIT 1;";

        using var conn = _factory.Create();
        var row = await conn.QuerySingleOrDefaultAsync<(string ID, string SlackChannelID, string Alias)>(sql, new { ID = channelId.ToString("D") });

        if (row.ID is null)
            return null;

        var result = new Channel
        {
            ID = string.IsNullOrWhiteSpace(row.ID)
                ? Guid.Empty
                : Guid.Parse(row.ID),
            SlackChannelID = row.SlackChannelID,
            Alias = row.Alias
        };

        return result;
    }

    public async Task<Channel?> GetChannel(string channelId)
    {
        if (_factory == null) return null;

        const string sql = @"SELECT ID, SlackChannelID, Alias FROM Channel WHERE SlackChannelID = @ID LIMIT 1;";

        using var conn = _factory.Create();
        var row = await conn.QuerySingleOrDefaultAsync<(string ID, string SlackChannelID, string Alias)>(sql, new { ID = channelId });

        if (row.ID is null)
            return null;

        var result = new Channel
        {
            ID = string.IsNullOrWhiteSpace(row.ID)
                ? Guid.Empty
                : Guid.Parse(row.ID),
            SlackChannelID = row.SlackChannelID,
            Alias = row.Alias
        };

        return result;
    }

    public async Task<List<AttachedAttachment>> GetAttachments(Guid messageId)
    {
        if (_factory == null) return new List<AttachedAttachment>();

        const string sql = @"
SELECT
  ID,
  MessageID,
  Type,
  Path,
  Title,
  FieldID,
  ContentText,
  ContentBin,
FROM AttachedAttachment
WHERE MessageID = @MessageID
";

        using var conn = _factory.Create();
        var rows = await conn.QueryAsync<(
            string ID,
            string? MessageID,
            int Type,
            string Path,
            string? Title,
            string? ContentText,
            byte[]? ContentBin)>
        (
            sql,
            new { MessageId = messageId.ToString("D") }
        );

        var result = rows
            .Select(r => new AttachedAttachment
            {
                ID = string.IsNullOrWhiteSpace(r.MessageID)
                    ? Guid.Empty
                    : Guid.Parse(r.ID),
                MessageID = string.IsNullOrWhiteSpace(r.MessageID)
                    ? Guid.Empty
                    : Guid.Parse(r.MessageID),
                Type = (Enums.AttachmentType)r.Type,
                Path = r.Path,
                Title = r.Title,
                ContentText = r.ContentText,
                ContentBin = r.ContentBin
            })
            .ToList();

        return result;
    }

    public async Task<int> GetAttachmentsNumber(Guid messageId)
    {
        if (_factory == null) return 0;

        const string sql = @"SELECT COUNT(*) FROM AttachedAttachment WHERE MessageID = @MessageID";

        using var conn = _factory.Create();
        var count = await conn.QuerySingleAsync<int>(sql, new { MessageID = messageId.ToString("D") });

        return count;
    }
    #endregion

    #region INSERT
    public async Task AddBundle(Bundle bundle)
    {
        if (bundle.ID == Guid.Empty)
            bundle.ID = Guid.NewGuid();

        const string sql = @"INSERT INTO Bundle (ID, Alias) VALUES (@ID, @Alias);";

        using var conn = _factory.Create();
        await conn.ExecuteAsync(sql, new
        {
            ID = bundle.ID.ToString("D"),
            bundle.Alias
        });
    }

    public async Task AddChannel(Channel channel)
    {
        if (channel.ID == Guid.Empty)
            channel.ID = Guid.NewGuid();

        const string sql = @"INSERT INTO Channel (ID, SlackChannelID, Alias) VALUES (@ID, @SlackChannelID, @Alias);";

        using var conn = _factory.Create();
        await conn.ExecuteAsync(sql, new
        {
            ID = channel.ID.ToString("D"),
            channel.SlackChannelID,
            channel.Alias
        });
    }

    public async Task AddBundleChannelMapping(BundleChannelMapping mapping)
    {
        if (mapping.ID == Guid.Empty)
            mapping.ID = Guid.NewGuid();

        const string sql = @"INSERT INTO BundleChannelMapping (ID, BundleID, ChannelID) VALUES (@ID, @BundleID, @ChannelID);";

        using var conn = _factory.Create();
        await conn.ExecuteAsync(sql, new
        {
            ID = mapping.ID.ToString("D"),
            BundleID = mapping.BundleID.ToString("D"),
            ChannelID = mapping.ChannelID.ToString("D")
        });
    }

    public async Task AddAttachedBundle(AttachedBundle bundle)
    {
        if (bundle.ID == Guid.Empty)
            bundle.ID = Guid.NewGuid();

        const string sql = @"INSERT INTO AttachedBundle (ID, MessageID, BundleID) VALUES (@ID, @MessageID, @BundleID);";

        using var conn = _factory.Create();
        await conn.ExecuteAsync(sql, new
        {
            ID = bundle.ID.ToString("D"),
            MessageID = bundle.MessageID.ToString("D"),
            BundleID = bundle.BundleID.ToString("D")
        });
    }

    public async Task AddAttachedChannel(AttachedChannel channel)
    {
        if (channel.ID == Guid.Empty)
            channel.ID = Guid.NewGuid();

        const string sql = @"INSERT INTO AttachedChannel (ID, MessageID, ChannelID) VALUES (@ID, @MessageID, @ChannelID);";

        using var conn = _factory.Create();
        await conn.ExecuteAsync(sql, new
        {
            ID = channel.ID.ToString("D"),
            MessageID = channel.MessageID.ToString("D"),
            ChannelID = channel.ChannelID.ToString("D")
        });
    }

    public async Task AddMessage(Message message)
    {
        if (message.ID == Guid.Empty)
            message.ID = Guid.NewGuid();

        const string sql = @"INSERT INTO Message (ID, Text, InsertDT, ScheduleDT, SendingDT, Managed) VALUES (@ID, @Text, @InsertDT, @ScheduleDT, @SendingDT, @Managed);";

        using var conn = _factory.Create();
        await conn.ExecuteAsync(sql, new
        {
            ID = message.ID.ToString("D"),
            message.Text,
            InsertDT = message.InsertDT.ToString("o"),
            ScheduleDT = message.ScheduleDT.HasValue ? message.ScheduleDT.Value.ToString("o") : null,
            SendingDT = message.SendingDT.HasValue ? message.SendingDT.Value.ToString("o") : null,
            Managed = message.Managed ? 1 : 0
        });
    }

    public async Task AddSendingError(SendingError error)
    {
        if (error.ID == Guid.Empty)
            error.ID = Guid.NewGuid();

        const string sql = @"INSERT INTO SendingError (ID, MessageID, ChannelID, Details, Type) VALUES (@ID, @MessageID, @ChannelID, @Details, @Type);";

        using var conn = _factory.Create();
        await conn.ExecuteAsync(sql, new
        {
            ID = error.ID.ToString("D"),
            error.Message,
            ChannelID = error.ChannelID.ToString("D"),
            error.Details,
            error.Type
        });
    }

    public async Task AddAttachment(AttachedAttachment attachment)
    {
        const string sql = @"
INSERT INTO AttachedAttachment
(
  ID,
  MessageID,
  Type,
  Path,
  Title,
  FieldID,
  ContentText,
  ContentBin
)
VALUES
(
  @ID,
  @MessageID,
  @Type,
  @Path,
  @Title,
  @FieldID,
  @ContentText,
  @ContentBin
);";

        if (attachment.ID.Equals(Guid.Empty))
            attachment.ID = Guid.NewGuid();

        using var conn = _factory.Create();
        await conn.ExecuteAsync(sql, new
        {
            ID = attachment.ID.ToString("D"),
            MessageID = attachment.MessageID.ToString("D"),
            Type = (int)attachment.Type,
            attachment.Path,
            attachment.Title,
            attachment.FieldID,
            attachment.ContentText,
            attachment.ContentBin
        });
    }
    #endregion

    #region UPDATE
    public async Task UpdateBundle(Bundle bundle)
    {
        const string sql = @"
UPDATE Bundle
SET Alias = @Alias
WHERE ID = @ID
;";

        using var conn = _factory.Create();
        var res = await conn.ExecuteAsync(sql, new { ID = bundle.ID.ToString("D"), Alias = bundle.Alias });
    }

    public async Task UpdateChannel(Channel channel)
    {
        const string sql = @"
UPDATE Channel
SET Alias = @Alias,
    SlackChannelID = @SlackChannelID
WHERE ID = @ID
;";
        
        using var conn = _factory.Create();
        await conn.ExecuteAsync(sql, new { ID = channel.ID.ToString("D"), Alias = channel.Alias, SlackChannelID = channel.SlackChannelID });
    }

    public async Task UpdateMessage(Message message)
    {
        const string sql1 = @"
UPDATE Message
SET Text = @Text,
    ScheduleDT = @ScheduleDT,
    SendingDT = @SendingDT,
    Managed = @Managed,
    LastUpdateDT = @LastUpdateDT
WHERE ID = @ID
;";

        const string sql2 = @"SELECT ID FROM AttachedChannel WHERE MessageID = @MessageID;";
        
        const string sql3 = @"DELETE FROM AttachedChannel WHERE ID = @ID;";
        
        const string sql3b = @"DELETE FROM AttachedChannel WHERE MessageID = @MessageID;";

        const string sql4 = @"
INSERT INTO AttachedChannel (
    ID,
    MessageID,
    ChannelID
)
VALUES
(
    @ID,
    @MessageID,
    @ChannelID
);";

        const string sql5 = @"SELECT ID FROM AttachedBundle WHERE MessageID = @MessageID;";

        const string sql6 = @"DELETE FROM AttachedBundle WHERE ID = @ID;";
        
        const string sql6b = @"DELETE FROM AttachedBundle WHERE MessageID = @MessageID;";

        const string sql7 = @"
INSERT INTO AttachedBundle (
    ID,
    MessageID,
    BundleID
)
VALUES
(
    @ID,
    @MessageID,
    @BundleID
);";

        const string sql8 = @"SELECT ID FROM AttachedAttachment WHERE MessageID = @MessageID;";

        const string sql9 = @"DELETE FROM AttachedAttachment WHERE ID = @ID;";
        
        const string sql9b = @"DELETE FROM AttachedAttachment WHERE MessageID = @MessageID;";

        const string sql10 = @"
INSERT INTO AttachedAttachment
(
  ID,
  MessageID,
  Type,
  Path,
  Title,
  FieldID,
  ContentText,
  ContentBin
)
VALUES
(
  @ID,
  @MessageID,
  @Type,
  @Path,
  @Title,
  @FieldID,
  @ContentText,
  @ContentBin
);";

        using var conn = _factory.Create();
        if (conn.State != ConnectionState.Open)
        {
            conn.Open();
        }

        using var tx = conn.BeginTransaction();

        try
        {
            await conn.ExecuteAsync(sql1, new
            {
                ID = message.ID.ToString("D"),
                message.Text,
                message.ScheduleDT,
                message.SendingDT,
                Managed = message.SendingDT.HasValue,
                LastUpdateDT = DateTime.Now
            });

            if (message.Channels?.Any() ?? false)
            {
                foreach (var channel in message.Channels.Where(p => p.ID.Equals(Guid.Empty)))
                {
                    const string sql = "SELECT ID FROM AttachedChannel WHERE MessageID = @MessageID AND ChannelID = @ChannelID LIMIT 1;";
                    var id = await conn.QueryAsync<string>(sql, new { MessageID = message.ID.ToString("D"), ChannelID = channel.Channel!.ID.ToString("D") });
                    channel.ID = id?.Any() ?? false ? Guid.Parse(id.First()) : Guid.NewGuid();
                }

                var channelsInDb = await conn.QueryAsync<string>(sql2, new { MessageID = message.ID.ToString("D") });
                var channelsInDbForBeingDeleted = channelsInDb.Where(p => !message.Channels.Any(q => q.ID.Equals(Guid.Parse(p))));
                foreach (var channel in channelsInDbForBeingDeleted)
                {
                    await conn.ExecuteAsync(sql3, new { ID = channel });
                }

                var channelsInDbForBeingInserted = message.Channels.Where(p => !channelsInDb.Any(q => q.Equals(p.ID.ToString("D"), StringComparison.InvariantCultureIgnoreCase)));
                foreach (var channel in channelsInDbForBeingInserted)
                {
                    await conn.ExecuteAsync(sql4, new
                    {
                        ID = Guid.NewGuid().ToString("D"),
                        MessageID = channel.MessageID.ToString("D"),
                        ChannelID = channel.ChannelID.ToString("D")
                    });
                }
            }
            else
            {
                await conn.ExecuteAsync(sql3b, new { MessageID = message.ID.ToString("D") });
            }

            if (message.Bundles?.Any() ?? false)
            {
                foreach (var bundle in message.Bundles.Where(p => p.ID.Equals(Guid.Empty)))
                {
                    const string sql = "SELECT ID FROM AttachedBundle WHERE MessageID = @MessageID AND BundleID = @BundleID LIMIT 1;";
                    var id = await conn.QueryAsync<string>(sql, new { MessageID = message.ID.ToString("D"), BundleID = bundle.Bundle!.ID.ToString("D") });
                    bundle.ID = id?.Any() ?? false ? Guid.Parse(id.First()) : Guid.NewGuid();
                }

                var bundlesInDb = await conn.QueryAsync<string>(sql5, new { MessageID = message.ID.ToString("D") });

                var bundlesInDbForBeingDeleted = bundlesInDb.Where(p => !message.Bundles.Any(q => q.ID.Equals(Guid.Parse(p))));
                foreach (var bundle in bundlesInDbForBeingDeleted)
                {
                    await conn.ExecuteAsync(sql6, new { ID = bundle });
                }

                var bundlesInDbForBeingInserted = message.Bundles.Where(p => !bundlesInDb.Any(q => q.Equals(p.ID.ToString("D"), StringComparison.InvariantCultureIgnoreCase)));
                foreach (var bundle in bundlesInDbForBeingInserted)
                {
                    await conn.ExecuteAsync(sql7, new
                    {
                        ID = Guid.NewGuid().ToString("D"),
                        MessageID = bundle.MessageID.ToString("D"),
                        BundleID = bundle.BundleID.ToString("D")
                    });
                }
            }
            else
            {
                await conn.ExecuteAsync(sql6b, new { MessageID = message.ID.ToString("D") });
            }

            if (message.Attachments?.Any() ?? false)
            {
                //foreach (var attachment in message.Attachments.Where(p => p.ID.Equals(Guid.Empty)))
                //{
                //    const string sql = "SELECT ID FROM AttachedAttachment WHERE MessageID = @MessageID AND BundleID = @BundleID LIMIT 1;";
                //    var id = await conn.QueryAsync<string>(sql, new { MessageID = message.ID.ToString("D") });
                //    attachment.ID = id?.Any() ?? false ? Guid.Parse(id.First()) : Guid.NewGuid();
                //}

                var attachmentsInDb = await conn.QueryAsync<string>(sql8, new { MessageID = message.ID.ToString("D") });

                var attachmentsInDbForBeingDeleted = attachmentsInDb.Where(p => !message.Attachments.Any(q => q.ID.Equals(Guid.Parse(p))));
                foreach (var channel in attachmentsInDbForBeingDeleted)
                {
                    await conn.ExecuteAsync(sql9, new { ID = channel });
                }

                var attachmentsInDbForBeingInserted = message.Attachments.Where(p => !attachmentsInDb.Any(q => q.Equals(p.ID.ToString("D"), StringComparison.InvariantCultureIgnoreCase)));
                foreach (var attachment in attachmentsInDbForBeingInserted)
                {
                    await conn.ExecuteAsync(sql10, new
                    {
                        ID = Guid.NewGuid().ToString("D"),
                        MessageID = attachment.MessageID.ToString("D"),
                        Type = (int)attachment.Type,
                        attachment.Path,
                        attachment.Title,
                        attachment.FieldID,
                        attachment.ContentText,
                        attachment.ContentBin
                    });
                }
            }
            else
            {
                await conn.ExecuteAsync(sql9b, new { MessageID = message.ID.ToString("D") });
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task UpdateMessageSetManaged(Guid id, DateTime sendingDT)
    {
        const string sql = @"
UPDATE Message
SET SendingDT = @SendingDT,
    Managed = 1,
    LastUpdateDT = @LastUpdateDT
WHERE ID = @ID
;";

        using var conn = _factory.Create();

        await conn.ExecuteAsync(sql, new
        {
            ID = id.ToString("D"),
            SendingDT = sendingDT.ToString("o"),
            LastUpdateDT = DateTime.Now
        });
    }

    #endregion

    #region DELETE
    public async Task DeleteBundle(Guid id)
    {
        const string sql1 = @"DELETE FROM Bundle WHERE ID = @ID;";
        const string sql2 = @"DELETE FROM BundleChannelMapping WHERE BundleID = @BundleID;";

        using var conn = _factory.Create();
        await conn.ExecuteAsync(sql1, new { ID = id.ToString("D") });
        await conn.ExecuteAsync(sql2, new { BundleID = id.ToString("D") });
    }

    public async Task DeleteChannel(Guid id, Guid? bundleId)
    {
        const string sql1 = @"DELETE FROM BundleChannelMapping WHERE ChannelID = @ChannelID AND BundleID = @BundleID;";
        const string sql2 = @"DELETE FROM BundleChannelMapping WHERE ChannelID = @ChannelID;";
        const string sql3 = @"DELETE FROM Channel WHERE ID = @ID;";

        using var conn = _factory.Create();

        if (bundleId.HasValue)
        {
            await conn.ExecuteAsync(sql1, new { ChannelID = id.ToString("D"), BundleID = bundleId.Value.ToString("D") });
        }
        else
        {
            await conn.ExecuteAsync(sql2, new { ChannelID = id.ToString("D") });
            await conn.ExecuteAsync(sql3, new { ID = id.ToString("D") });
        }
    }

    public async Task DeleteMessage(Guid id)
    {
        var sql = new[]
        {
            @"DELETE FROM AttachedAttachment WHERE MessageID = @ID;",
            @"DELETE FROM AttachedBundle WHERE MessageID = @ID;",
            @"DELETE FROM AttachedChannel WHERE MessageID = @ID;",
            @"DELETE FROM SendingError WHERE MessageID = @ID;",
            @"DELETE FROM Message WHERE ID = @ID;"
        };

        using var conn = _factory.Create();
        if (conn.State != ConnectionState.Open)
        {
            conn.Open();
        }

        using var tx = conn.BeginTransaction();

        try
        {
            for (var i = 0; i < sql.Length; ++i)
            {
                await conn.ExecuteAsync(
                    sql[i],
                    new { ID = id.ToString("D") },
                    tx
                );
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
    #endregion
}
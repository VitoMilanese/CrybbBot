namespace DataLayer.SQL;

internal class DB_SQLITE
{
    public const string CreateSql = @"
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Bundle
(
    ID    TEXT NOT NULL PRIMARY KEY,
    Alias TEXT NULL
);

CREATE TABLE IF NOT EXISTS Channel
(
    ID             TEXT NOT NULL PRIMARY KEY,
    SlackChannelID TEXT NOT NULL,
    Alias          TEXT NULL,
    UNIQUE(SlackChannelID)
);

CREATE TABLE IF NOT EXISTS Message
(
    ID         TEXT NOT NULL PRIMARY KEY,
    Text       TEXT NOT NULL,
    InsertDT   TEXT NOT NULL,   -- ISO8601 recommended
    ScheduleDT TEXT NULL,
    SendingDT  TEXT NULL,
    Managed    INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS BundleChannelMapping
(
    ID        TEXT NOT NULL PRIMARY KEY,
    BundleID  TEXT NOT NULL,
    ChannelID TEXT NOT NULL,
    FOREIGN KEY(BundleID) REFERENCES Bundle(ID) ON DELETE CASCADE,
    FOREIGN KEY(ChannelID) REFERENCES Channel(ID) ON DELETE CASCADE,
    UNIQUE(BundleID, ChannelID)
);

CREATE TABLE IF NOT EXISTS AttachedBundle
(
    ID        TEXT NOT NULL PRIMARY KEY,
    MessageID TEXT NOT NULL,
    BundleID  TEXT NOT NULL,
    FOREIGN KEY(MessageID) REFERENCES Message(ID) ON DELETE CASCADE,
    FOREIGN KEY(BundleID)  REFERENCES Bundle(ID),
    UNIQUE(MessageID, BundleID)
);

CREATE TABLE IF NOT EXISTS AttachedChannel
(
    ID        TEXT NOT NULL PRIMARY KEY,
    MessageID TEXT NOT NULL,
    ChannelID TEXT NOT NULL,
    FOREIGN KEY(MessageID) REFERENCES Message(ID) ON DELETE CASCADE,
    FOREIGN KEY(ChannelID) REFERENCES Channel(ID),
    UNIQUE(MessageID, ChannelID)
);

CREATE TABLE IF NOT EXISTS AttachedAttachment
(
    ID          TEXT NOT NULL PRIMARY KEY,
    MessageID   TEXT NOT NULL,
    Type        INTEGER NOT NULL,  -- AttachmentType enum
    Path        TEXT NOT NULL,
    Title       TEXT NULL,
    FieldID     TEXT NULL,
    ContentText TEXT NULL,
    ContentBin  BLOB NULL,
    FOREIGN KEY(MessageID) REFERENCES Message(ID) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS SendingError
(
    ID        TEXT NOT NULL PRIMARY KEY,
    MessageID TEXT NOT NULL,
    ChannelID TEXT NOT NULL,
    Details   TEXT NULL,
    Type      INTEGER NOT NULL,
    FOREIGN KEY(MessageID) REFERENCES Message(ID) ON DELETE CASCADE,
    FOREIGN KEY(ChannelID) REFERENCES Channel(ID)
);

-- Helpful indexes
CREATE INDEX IF NOT EXISTS IX_Message_InsertDT ON Message(InsertDT);
CREATE INDEX IF NOT EXISTS IX_Message_ScheduleDT ON Message(ScheduleDT);
CREATE INDEX IF NOT EXISTS IX_AttachedAttachment_MessageID ON AttachedAttachment(MessageID);
CREATE INDEX IF NOT EXISTS IX_SendingError_MessageID ON SendingError(MessageID);
CREATE INDEX IF NOT EXISTS IX_SendingError_ChannelID ON SendingError(ChannelID);
";
}

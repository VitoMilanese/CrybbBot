namespace DataLayer.SQL;

internal class DB_MSSQL
{
    public const string CreateDb = @"
-- Create DB if not exists
IF DB_ID(N'{0}') IS NULL
BEGIN
    CREATE DATABASE [{0}];
END;
";

    public const string CreateTables = @"
-- =========================
-- Bundle
-- =========================
IF OBJECT_ID('dbo.Bundle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bundle
    (
        ID      UNIQUEIDENTIFIER NOT NULL,
        Alias   NVARCHAR(200) NULL,
        CONSTRAINT PK_Bundle PRIMARY KEY (ID)
    );
END;

-- =========================
-- Channel
-- =========================
IF OBJECT_ID('dbo.Channel', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Channel
    (
        ID              UNIQUEIDENTIFIER NOT NULL,
        SlackChannelID  NVARCHAR(100) NOT NULL,
        Alias           NVARCHAR(200) NULL,
        CONSTRAINT PK_Channel PRIMARY KEY (ID),
        CONSTRAINT UQ_Channel_SlackChannelID UNIQUE (SlackChannelID)
    );
END;

-- =========================
-- Message
-- =========================
IF OBJECT_ID('dbo.Message', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Message
    (
        ID          UNIQUEIDENTIFIER NOT NULL,
        [Text]      NVARCHAR(MAX) NOT NULL,
        InsertDT    DATETIME2 NOT NULL,
        ScheduleDT  DATETIME2 NULL,
        SendingDT   DATETIME2 NULL,
        Managed     BIT NOT NULL CONSTRAINT DF_Message_Managed DEFAULT (0),
        CONSTRAINT PK_Message PRIMARY KEY (ID)
    );
END;

-- =========================
-- BundleChannelMapping (Bundle <-> Channel)
-- =========================
IF OBJECT_ID('dbo.BundleChannelMapping', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BundleChannelMapping
    (
        ID          UNIQUEIDENTIFIER NOT NULL,
        BundleID    UNIQUEIDENTIFIER NOT NULL,
        ChannelID   UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_BundleChannelMapping PRIMARY KEY (ID),
        CONSTRAINT FK_BCM_Bundle FOREIGN KEY (BundleID) REFERENCES dbo.Bundle(ID) ON DELETE CASCADE,
        CONSTRAINT FK_BCM_Channel FOREIGN KEY (ChannelID) REFERENCES dbo.Channel(ID) ON DELETE CASCADE,
        CONSTRAINT UQ_BCM UNIQUE (BundleID, ChannelID)
    );
END;

-- =========================
-- AttachedBundle (Message <-> Bundle)
-- =========================
IF OBJECT_ID('dbo.AttachedBundle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AttachedBundle
    (
        ID          UNIQUEIDENTIFIER NOT NULL,
        MessageID   UNIQUEIDENTIFIER NOT NULL,
        BundleID    UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_AttachedBundle PRIMARY KEY (ID),
        CONSTRAINT FK_AB_Message FOREIGN KEY (MessageID) REFERENCES dbo.Message(ID) ON DELETE CASCADE,
        CONSTRAINT FK_AB_Bundle  FOREIGN KEY (BundleID)  REFERENCES dbo.Bundle(ID),
        CONSTRAINT UQ_AB UNIQUE (MessageID, BundleID)
    );
END;

-- =========================
-- AttachedChannel (Message <-> Channel)
-- =========================
IF OBJECT_ID('dbo.AttachedChannel', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AttachedChannel
    (
        ID          UNIQUEIDENTIFIER NOT NULL,
        MessageID   UNIQUEIDENTIFIER NOT NULL,
        ChannelID   UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_AttachedChannel PRIMARY KEY (ID),
        CONSTRAINT FK_AC_Message FOREIGN KEY (MessageID) REFERENCES dbo.Message(ID) ON DELETE CASCADE,
        CONSTRAINT FK_AC_Channel FOREIGN KEY (ChannelID) REFERENCES dbo.Channel(ID),
        CONSTRAINT UQ_AC UNIQUE (MessageID, ChannelID)
    );
END;

-- =========================
-- AttachedAttachment (Message -> Attachment)
-- AttachmentType stored as INT
-- =========================
IF OBJECT_ID('dbo.AttachedAttachment', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AttachedAttachment
    (
        ID          UNIQUEIDENTIFIER NOT NULL,
        MessageID   UNIQUEIDENTIFIER NOT NULL,
        [Type]      INT NOT NULL,
        [Path]      NVARCHAR(1000) NOT NULL,
        Title       NVARCHAR(400) NULL,
        FieldID     NVARCHAR(200) NULL,
        ContentText NVARCHAR(MAX) NULL,
        ContentBin  VARBINARY(MAX) NULL,
        CONSTRAINT PK_AttachedAttachment PRIMARY KEY (ID),
        CONSTRAINT FK_AA_Message FOREIGN KEY (MessageID) REFERENCES dbo.Message(ID) ON DELETE CASCADE
    );
END;

-- =========================
-- SendingError (Message <-> Channel)
-- =========================
IF OBJECT_ID('dbo.SendingError', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SendingError
    (
        ID          UNIQUEIDENTIFIER NOT NULL,
        MessageID   UNIQUEIDENTIFIER NOT NULL,
        ChannelID   UNIQUEIDENTIFIER NOT NULL,
        Details     NVARCHAR(MAX) NULL,
        [Type]      INT NOT NULL,
        CONSTRAINT PK_SendingError PRIMARY KEY (ID),
        CONSTRAINT FK_SE_Message FOREIGN KEY (MessageID) REFERENCES dbo.Message(ID) ON DELETE CASCADE,
        CONSTRAINT FK_SE_Channel FOREIGN KEY (ChannelID) REFERENCES dbo.Channel(ID)
    );
END;

-- Helpful indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Message_InsertDT' AND object_id = OBJECT_ID('dbo.Message'))
    CREATE INDEX IX_Message_InsertDT ON dbo.Message(InsertDT);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Message_ScheduleDT' AND object_id = OBJECT_ID('dbo.Message'))
    CREATE INDEX IX_Message_ScheduleDT ON dbo.Message(ScheduleDT);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttachedAttachment_MessageID' AND object_id = OBJECT_ID('dbo.AttachedAttachment'))
    CREATE INDEX IX_AttachedAttachment_MessageID ON dbo.AttachedAttachment(MessageID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SendingError_MessageID' AND object_id = OBJECT_ID('dbo.SendingError'))
    CREATE INDEX IX_SendingError_MessageID ON dbo.SendingError(MessageID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SendingError_ChannelID' AND object_id = OBJECT_ID('dbo.SendingError'))
    CREATE INDEX IX_SendingError_ChannelID ON dbo.SendingError(ChannelID);
";
}

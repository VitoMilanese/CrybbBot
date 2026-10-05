namespace DataLayer.Enums
{
    public enum DbKind
    {
        None,
        SqlServer,
        Sqlite
    }

    public enum AttachmentType
    {
        Unknown = 0,
        Image = 1,
        File = 2,
        FileReference = 3
    }
}

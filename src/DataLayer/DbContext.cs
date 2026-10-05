using DataLayer.Enums;

namespace DataLayer
{
    public static class DbContext
    {
        public static Data Data { get; private set; } = new Data();

        public static void Init(DbKind dbKind, string connectionString)
        {
            var factory = new DbConnectionFactory(dbKind, connectionString);
            Data = new Data(factory);
        }
    }
}

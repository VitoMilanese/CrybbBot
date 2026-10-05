using System.Reflection;
using Dapper;
using Microsoft.Data.Sqlite;

namespace DataLayer;

public static class SqliteBootstrap
{
    private static string DbFilePath { get; }

    static SqliteBootstrap()
    {
        var oAssembly = Assembly.GetExecutingAssembly();
        var root = Path.GetDirectoryName(oAssembly.Location)!;
        DbFilePath = Path.Combine(root, "DB", "app.db");
    }

    public static async Task EnsureDbAsync()
    {
        // 1) Ensure directory exists
        var dir = Path.GetDirectoryName(DbFilePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        // 2) Opening connection creates the DB file if missing
        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = DbFilePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        await using var conn = new SqliteConnection(cs);
        await conn.OpenAsync();

        // 3) Recommended for relations
        await conn.ExecuteAsync("PRAGMA foreign_keys = ON;");

        // 4) Apply schema (safe to run multiple times)
        const string schema = SQL.DB_SQLITE.CreateSql;

        await conn.ExecuteAsync(schema);
    }

    //public static async Task<List<Message>> GetAllMessages(DateTime from)
    //{
    //    const string sql = @"SELECT ID, Alias FROM Bundle ORDER BY Alias;";

    //    using var conn = _factory.Create();
    //    var rows = await conn.QueryAsync<(string ID, string? Alias)>(sql);

    //    var result = rows
    //        .Select(r => new Bundle
    //        {
    //            ID = Guid.Parse(r.ID),
    //            Alias = r.Alias
    //        })
    //        .ToList();

    //    return new List<Message>();
    //}
}
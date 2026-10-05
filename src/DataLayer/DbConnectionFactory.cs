using System.Data;
using DataLayer.Enums;
using DataLayer.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace DataLayer;

public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _cs;
    public DbKind Kind { get; }

    public DbConnectionFactory(DbKind kind, string connectionString)
    {
        Kind = kind;
        _cs = connectionString;
    }

    public IDbConnection Create()
        => Kind switch
        {
            DbKind.SqlServer => new SqlConnection(_cs),
            DbKind.Sqlite => new SqliteConnection(_cs),
            _ => throw new NotSupportedException()
        };
}

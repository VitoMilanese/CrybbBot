using System.Data;
using Dapper;

namespace DataLayer;

public sealed class SqliteGuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value)
    {
        if (value is Guid g) return g;

        // SQLite TEXT will come as string
        if (value is string s && Guid.TryParse(s, out var parsed))
            return parsed;

        // Sometimes it comes as byte[] if you stored BLOB(16)
        if (value is byte[] bytes && bytes.Length == 16)
            return new Guid(bytes);

        throw new DataException($"Cannot convert {value?.GetType().Name} to Guid: {value}");
    }

    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        // store as TEXT
        parameter.DbType = DbType.String;
        parameter.Value = value.ToString("D");
    }
}
using System.Data;
using DataLayer.Enums;

namespace DataLayer.Interfaces;

public interface IDbConnectionFactory
{
    IDbConnection Create();
    DbKind Kind { get; }
}

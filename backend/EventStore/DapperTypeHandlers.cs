using Dapper;

namespace CCC.EventStore;

// Dapper keeps type handlers process-wide, so every type that opens connections for
// Dapper registers them from its static constructor, before its first query.
public static class DapperTypeHandlers
{
    public static void Register()
    {
        SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());
        SqlMapper.AddTypeHandler(new StringArrayTypeHandler());
    }
}

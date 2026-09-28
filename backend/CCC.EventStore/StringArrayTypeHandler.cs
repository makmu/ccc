using System.Data;
using Dapper;

namespace CCC.EventStore;

// Postgres text[] <-> string[]. Dapper does not map array columns on its own (it
// sees the reader's field type as System.Array and refuses to bind the
// constructor parameter), and without a handler it would also expand a string[]
// parameter into a comma-separated list, which breaks "tags @> @tags".
class StringArrayTypeHandler : SqlMapper.TypeHandler<string[]>
{
    public override void SetValue(IDbDataParameter parameter, string[]? value)
        => parameter.Value = value ?? [];

    public override string[] Parse(object value)
        => value as string[] ?? ((IEnumerable<object>)value).Select(v => v.ToString()!).ToArray();
}

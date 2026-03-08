using System.Data;
using Dapper;

namespace CCC.Infrastructure.EventStore;

class DateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        => parameter.Value = value;

    public override DateTimeOffset Parse(object value)
        => value is DateTime dt
            ? new DateTimeOffset(dt, TimeSpan.Zero)
            : (DateTimeOffset)value;
}

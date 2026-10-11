using System.Text.Json;

namespace CCC.Dcb;

public static class EventRecordExtensions
{
    public static T PayloadAs<T>(this EventRecord record) =>
        JsonSerializer.Deserialize<T>(record.Payload)
        ?? throw new InvalidOperationException($"Failed to deserialize event of type {typeof(T).Name}");
}

namespace CCC.Base.Dcb;

public record EventRecord(long Position, string Type, string Payload, DateTimeOffset Timestamp, Guid User, string[] Tags);

namespace CCC.Infrastructure.EventStore;

record EventRecord(long Position, string Type, string Payload, DateTimeOffset Timestamp, Guid User);

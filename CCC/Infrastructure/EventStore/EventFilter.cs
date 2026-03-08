namespace CCC.Infrastructure.EventStore;

record EventFilter(string[] Types, Dictionary<string, string> PayloadProperties);

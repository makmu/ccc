namespace CCC.Infrastructure.EventStore;

class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }

    public ConcurrencyException(string message, Exception innerException) : base(message, innerException) { }
}

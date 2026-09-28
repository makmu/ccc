using System.Threading.Channels;

namespace CCC.EventStore;

sealed class AppendSignal
{
    private readonly Lock _lock = new();
    private readonly List<Channel<bool>> _channels = [];

    // A pending signal already means "catch up", so further appends before the
    // subscriber wakes up are coalesced into it.
    public ChannelReader<bool> Subscribe()
    {
        var channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
        });

        lock (_lock)
            _channels.Add(channel);

        return channel.Reader;
    }

    public void Notify()
    {
        lock (_lock)
            foreach (var channel in _channels)
                channel.Writer.TryWrite(true);
    }
}

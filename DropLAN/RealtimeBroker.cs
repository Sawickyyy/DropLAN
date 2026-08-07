using System.Collections.Concurrent;
using System.Threading.Channels;

namespace DropLAN;

public sealed class RealtimeBroker
{
    private readonly ConcurrentDictionary<Guid, Channel<string>> _clients = new();

    public int ClientCount => _clients.Count;

    public ChannelReader<string> Subscribe(CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        var channel = Channel.CreateUnbounded<string>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        _clients[id] = channel;

        cancellationToken.Register(() =>
        {
            if (_clients.TryRemove(id, out var removed))
                removed.Writer.TryComplete();
        });

        return channel.Reader;
    }

    public void Publish(string eventName = "state")
    {
        foreach (var channel in _clients.Values)
            channel.Writer.TryWrite(eventName);
    }
}

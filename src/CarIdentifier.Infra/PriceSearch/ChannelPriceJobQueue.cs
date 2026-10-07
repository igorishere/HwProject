using CarIdentifier.Application.Abstraction.PriceFinder;
using System.Threading.Channels;

namespace CarIdentifier.Infra.PriceSearch;

public sealed class ChannelPriceJobQueue : IPriceJobQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(jobId, cancellationToken);

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAsync(cancellationToken);
}

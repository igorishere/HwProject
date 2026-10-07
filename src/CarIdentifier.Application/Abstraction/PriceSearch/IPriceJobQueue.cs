namespace CarIdentifier.Application.Abstraction.PriceFinder;

public interface IPriceJobQueue
{
    ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default);

    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken = default);
}

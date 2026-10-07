namespace CarIdentifier.Application.Abstraction.PriceFinder;

public interface IPriceJobStore
{
    Task AddAsync(PriceSearchJob job, CancellationToken cancellationToken = default);

    Task<PriceSearchJob?> GetAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task UpdateAsync(PriceSearchJob job, CancellationToken cancellationToken = default);

    Task RemoveExpiredAsync(CancellationToken cancellationToken = default);
}

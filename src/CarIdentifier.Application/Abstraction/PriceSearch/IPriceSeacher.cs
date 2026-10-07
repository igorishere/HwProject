using CarIdentifier.Application.Contracts;

namespace CarIdentifier.Application.Abstraction.PriceFinder;

public interface IPriceSearcher
{
    Task<PriceSearchResult?> FindPriceAsync(
        PriceSearchRequest carIdentification,
        CancellationToken cancellationToken = default);
}
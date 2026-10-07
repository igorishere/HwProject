using CarIdentifier.Application.Abstraction.PriceFinder;

namespace CarIdentifier.Application.Contracts;

public sealed record CarResult(
    CarIdentificationResult? CarIdentification = null,
    PriceSearchResult? PriceSearchResult = null,
    PriceSearchJobStatus? PriceSearchStatus = null,
    DateTimeOffset? PriceSearchStartedAt = null,
    DateTimeOffset? PriceSearchCompletedAt = null,
    string? PriceSearchFailureReason = null);
using CarIdentifier.Application.Contracts;

namespace CarIdentifier.Application.Abstraction.PriceFinder;

public sealed record PriceSearchJob(
    Guid Id,
    CarIdentificationResult CarIdentification,
    ReadOnlyMemory<byte> Image,
    string MediaType,
    PriceSearchJobStatus Status,
    PriceSearchResult? Result,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);

namespace CarIdentifier.Application.Contracts;

public sealed record CarIdentificationResult(
    string? Name,
    int? YearOfRelease,
    string? Manufacturer,
    string? Model,
    Guid? PriceSearchJobId = null
);

namespace CarIdentifier.Application.Abstraction.PriceFinder;

public record PriceSearchRequest(
    string? Name,
    int? YearOfRelease,
    string? Manufacturer,
    string? Model,
    ImageData ImageData
);

public record ImageData(
    Stream ImageStream,
    string MediaType
);
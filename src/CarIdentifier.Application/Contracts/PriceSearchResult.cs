namespace CarIdentifier.Application.Contracts;

public sealed record PriceSearchResult(
decimal? MinimumPrice,
decimal? MaximumPrice,
string? Currency,
decimal? AveragePrice,
IReadOnlyList<PriceSource> Sources);

public record PriceSource(string Name, string Url);
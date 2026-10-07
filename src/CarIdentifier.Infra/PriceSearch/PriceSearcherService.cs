using CarIdentifier.Application.Abstraction.PriceFinder;
using CarIdentifier.Application.Contracts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarIdentifier.Infra;

public sealed class PriceSearcherService(
    IChatClient chatClient,
    ILogger<PriceSearcherService> logger) : IPriceSearcher
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private const string Instructions = """
        Search the web for current asking prices for the exact Hot Wheels release described by the user.

        Use the supplied image to confirm the release and variant when possible. Prioritize active
        listings from Brazilian marketplaces and compare only listings for the same release and
        variant. Do not mix asking prices with sold prices or price-guide values. Do not include
        shipping costs. Use prices in BRL only; if comparable BRL listings cannot be found, return
        null price values rather than converting or mixing currencies.

        Use at least two independent sources when available. Only include sources you actually
        opened and used to find a comparable listing. Do not invent prices, listings, URLs, or
        release details. If there are no reliable comparable listings, return null for all price
        values and an empty sources list.

        minimumPrice, maximumPrice, and averagePrice must be calculated from the comparable active
        asking prices found. Return the average as their arithmetic mean.
        """;

    public async Task<PriceSearchResult?> FindPriceAsync(
        PriceSearchRequest carIdentification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(carIdentification);
        ArgumentNullException.ThrowIfNull(carIdentification.ImageData);
        ArgumentNullException.ThrowIfNull(carIdentification.ImageData.ImageStream);

        var imageContent = await DataContent.LoadFromAsync(
            carIdentification.ImageData.ImageStream,
            carIdentification.ImageData.MediaType,
            cancellationToken);

        var message = new ChatMessage(
            ChatRole.User,
            [
                new TextContent(
                    $"""
                    Find current asking prices for this Hot Wheels release:
                    Name: {carIdentification.Name ?? "Unknown"}
                    Release year: {carIdentification.YearOfRelease?.ToString() ?? "Unknown"}
                    Real-car manufacturer: {carIdentification.Manufacturer ?? "Unknown"}
                    Real-car model: {carIdentification.Model ?? "Unknown"}
                    """),
                imageContent
            ]);

        var options = new ChatOptions
        {
            Instructions = Instructions,
            Reasoning = new ReasoningOptions { Effort = ReasoningEffort.Low },
            ResponseFormat = ChatResponseFormat.ForJsonSchema<PriceSearchResult>(
                SerializerOptions,
                schemaName: "hot_wheels_price_search",
                schemaDescription:
                    "Comparable current Brazilian asking prices and the web sources used to find them."),
#pragma warning disable OPENAI001
            Tools = [new WebSearchTool().AsAITool()]
#pragma warning restore OPENAI001
        };

        var stopwatch = Stopwatch.StartNew();
        long? firstTextMs = null;
        var text = new StringBuilder();

        await foreach (var update in chatClient.GetStreamingResponseAsync(
            message, options, cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                firstTextMs ??= stopwatch.ElapsedMilliseconds;
                text.Append(update.Text);
            }
        }

        logger.LogInformation(
            "AI price search: first text {FirstTextMs}ms, complete {CompleteMs}ms",
            firstTextMs,
            stopwatch.ElapsedMilliseconds);

        if (text.Length == 0)
        {
            throw new InvalidOperationException(
                "The AI provider returned no content for the price search.");
        }

        var result = JsonSerializer.Deserialize<PriceSearchResult>(
            text.ToString(),
            SerializerOptions);

        if (result is null)
        {
            throw new InvalidOperationException(
                "The AI provider returned an empty price search.");
        }

        if (result.Sources is null)
        {
            throw new InvalidOperationException(
                "The AI provider returned no sources for the price search.");
        }

        return result;
    }
}

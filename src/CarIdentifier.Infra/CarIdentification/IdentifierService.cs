using CarIdentifier.Application.Abstraction.CarIdentification;
using CarIdentifier.Application.Contracts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarIdentifier.Infra;

public sealed class IdentifierService(
    IChatClient chatClient,
    ILogger<IdentifierService> logger) : ICarIdentifier
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private const string Instructions = """
        Identify the exact Hot Wheels release shown in this image.

        name - name of the Hot Wheels model.
        yearOfRelease - year of the specific Hot Wheels release shown in the photo.
        manufacturer - manufacturer of the real car.
        model - model of the real car.

        Use visible information from the card and packaging whenever available.

        The yearOfRelease is NOT:
        - the year the real car was manufactured;
        - the year the real car model was introduced;
        - the year the Hot Wheels casting was originally introduced.

        If the specific release year cannot be determined reliably from the image, return null.
        Do not guess or invent information.
        """;

    public async Task<CarIdentificationResult> IdentifyAsync(
        Stream image,
        string mediaType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);

        var imageContent = await DataContent.LoadFromAsync(
            image,
            mediaType,
            cancellationToken);

        var message = new ChatMessage(
            ChatRole.User,
            [
                new TextContent("Identify this Hot Wheels product."),
                imageContent
            ]);

        var options = new ChatOptions
        {
            Instructions = Instructions,
            Reasoning = new ReasoningOptions { Effort = ReasoningEffort.Low },
            ResponseFormat = ChatResponseFormat.ForJsonSchema<AiCarIdentification>(
                SerializerOptions,
                schemaName: "car_identification",
                schemaDescription:
                    "Identification of a Hot Wheels product based only on visible packaging information.")
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
            "AI identification: first text {FirstTextMs}ms, complete {CompleteMs}ms",
            firstTextMs,
            stopwatch.ElapsedMilliseconds);

        if (text.Length == 0)
        {
            throw new InvalidOperationException(
                "The AI provider returned no content.");
        }

        var identification = JsonSerializer.Deserialize<AiCarIdentification>(
            text.ToString(),
            SerializerOptions);

        return identification is not null
            ? new CarIdentificationResult(
                identification.Name,
                identification.YearOfRelease,
                identification.Manufacturer,
                identification.Model)
            : throw new InvalidOperationException(
                "The AI provider returned an empty identification.");
    }
}

internal sealed record AiCarIdentification(
    string? Name,
    int? YearOfRelease,
    string? Manufacturer,
    string? Model);
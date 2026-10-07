using CarIdentifier.Application.Abstraction.PriceFinder;
using CarIdentifier.Infra;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Runtime.CompilerServices;

namespace CarIdentifier.UnitTests.CarIdentifier.Infra.PriceSearch;

public sealed class PriceSearcherServiceTests
{
    [Fact]
    public async Task FindPriceAsync_ShouldReturnStructuredPricesAndEnableWebSearch()
    {
        const string responseJson = """
            {
              "minimumPrice": 25.00,
              "maximumPrice": 45.00,
              "currency": "BRL",
              "averagePrice": 35.00,
              "sources": [
                { "name": "Marketplace A", "url": "https://example.com/a" },
                { "name": "Marketplace B", "url": "https://example.com/b" }
              ]
            }
            """;
        var chatClient = Substitute.For<IChatClient>();
        var service = new PriceSearcherService(
            chatClient,
            Substitute.For<ILogger<PriceSearcherService>>());
        ChatOptions? receivedOptions = null;
        IEnumerable<ChatMessage>? receivedMessages = null;

        chatClient.GetStreamingResponseAsync(
                Arg.Do<IEnumerable<ChatMessage>>(messages => receivedMessages = messages),
                Arg.Do<ChatOptions>(options => receivedOptions = options),
                Arg.Any<CancellationToken>())
            .Returns(StreamResponse(responseJson));

        var result = await service.FindPriceAsync(
            new PriceSearchRequest(
                "Twin Mill",
                2024,
                "Hot Wheels",
                "Twin Mill",
                new ImageData(new MemoryStream([1, 2, 3]), "image/jpeg")));

        Assert.NotNull(result);
        Assert.Equal(25.00m, result.MinimumPrice);
        Assert.Equal(45.00m, result.MaximumPrice);
        Assert.Equal(35.00m, result.AveragePrice);
        Assert.Equal("BRL", result.Currency);
        Assert.Equal(2, result.Sources.Count);
        Assert.NotNull(receivedOptions);
        Assert.Single(receivedOptions.Tools!);
        var userMessage = Assert.Single(receivedMessages!);
        Assert.Contains(userMessage.Contents, content => content is DataContent);
        Assert.Contains(
            "Twin Mill",
            Assert.Single(userMessage.Contents.OfType<TextContent>()).Text);
    }

    [Fact]
    public async Task FindPriceAsync_ShouldThrow_WhenProviderReturnsNoContent()
    {
        var chatClient = Substitute.For<IChatClient>();
        var service = new PriceSearcherService(
            chatClient,
            Substitute.For<ILogger<PriceSearcherService>>());

        chatClient.GetStreamingResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(StreamResponse(string.Empty));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FindPriceAsync(
                new PriceSearchRequest(
                    "Twin Mill",
                    2024,
                    "Hot Wheels",
                    "Twin Mill",
                    new ImageData(new MemoryStream([1, 2, 3]), "image/jpeg"))));
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamResponse(
        string text,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        if (text.Length > 0)
        {
            yield return new ChatResponseUpdate(
                ChatRole.Assistant,
                [new TextContent(text)]);
        }
    }
}

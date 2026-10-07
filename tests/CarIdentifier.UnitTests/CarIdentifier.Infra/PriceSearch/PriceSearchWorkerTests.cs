using CarIdentifier.Application.Abstraction.PriceFinder;
using CarIdentifier.Application.Contracts;
using CarIdentifier.Infra.PriceSearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CarIdentifier.UnitTests.CarIdentifier.Infra.PriceSearch;

public sealed class PriceSearchWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldCompleteQueuedJob()
    {
        var queue = new ChannelPriceJobQueue();
        var store = new InMemoryPriceJobStore();
        var jobId = Guid.NewGuid();
        var job = new PriceSearchJob(
            jobId,
            new CarIdentificationResult("Twin Mill", 2024, "Hot Wheels", "Twin Mill", jobId),
            new byte[] { 1, 2, 3 },
            "image/jpeg",
            PriceSearchJobStatus.Queued,
            null,
            null,
            DateTimeOffset.UtcNow,
            null,
            null);
        var expectedResult = new PriceSearchResult(20, 40, "BRL", 30, []);
        var priceSearcher = Substitute.For<IPriceSearcher>();
        byte[]? imageBytes = null;
        priceSearcher.FindPriceAsync(
                Arg.Do<PriceSearchRequest>(request =>
                {
                    using var imageBuffer = new MemoryStream();
                    request.ImageData.ImageStream.CopyTo(imageBuffer);
                    imageBytes = imageBuffer.ToArray();
                }),
                Arg.Any<CancellationToken>())
            .Returns(expectedResult);
        var worker = new PriceSearchWorker(
            queue,
            store,
            new TestScopeFactory(priceSearcher),
            NullLogger<PriceSearchWorker>.Instance);

        await store.AddAsync(job);
        await queue.EnqueueAsync(jobId);
        await worker.StartAsync(CancellationToken.None);

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            PriceSearchJob? completedJob = null;
            while (completedJob?.Status != PriceSearchJobStatus.Completed)
            {
                timeout.Token.ThrowIfCancellationRequested();
                completedJob = await store.GetAsync(jobId, timeout.Token);
                if (completedJob?.Status != PriceSearchJobStatus.Completed)
                {
                    await Task.Delay(10, timeout.Token);
                }
            }

            Assert.Equal(expectedResult, completedJob.Result);
            Assert.NotNull(completedJob.StartedAt);
            Assert.NotNull(completedJob.CompletedAt);
            Assert.Equal(new byte[] { 1, 2, 3 }, imageBytes);
            await priceSearcher.Received(1).FindPriceAsync(
                Arg.Is<PriceSearchRequest>(request =>
                    request.Name == "Twin Mill"
                    && request.ImageData.MediaType == "image/jpeg"
                    && request.ImageData.ImageStream.CanRead == false),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
            worker.Dispose();
        }
    }

    private sealed class TestScopeFactory(IPriceSearcher priceSearcher) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new TestScope(priceSearcher);
    }

    private sealed class TestScope(IPriceSearcher priceSearcher) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new TestServiceProvider(priceSearcher);

        public void Dispose()
        {
        }
    }

    private sealed class TestServiceProvider(IPriceSearcher priceSearcher) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(IPriceSearcher) ? priceSearcher : null;
    }
}

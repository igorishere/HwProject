using CarIdentifier.Application.Abstraction.PriceFinder;
using CarIdentifier.Application.Contracts;
using CarIdentifier.Infra.PriceSearch;

namespace CarIdentifier.UnitTests.CarIdentifier.Infra.PriceSearch;

public sealed class InMemoryPriceJobStoreTests
{
    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenPendingJobIsExpired()
    {
        var store = new InMemoryPriceJobStore();
        var job = CreateJob(
            PriceSearchJobStatus.Pending,
            DateTimeOffset.UtcNow.AddMinutes(-31));
        await store.AddAsync(job);

        var result = await store.GetAsync(job.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenCompletedJobIsExpired()
    {
        var store = new InMemoryPriceJobStore();
        var job = CreateJob(
            PriceSearchJobStatus.Completed,
            DateTimeOffset.UtcNow.AddHours(-25)) with
        {
            CompletedAt = DateTimeOffset.UtcNow.AddHours(-25)
        };
        await store.AddAsync(job);

        var result = await store.GetAsync(job.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnQueuedJobAndProtectStoredImageFromMutation()
    {
        var originalImage = new byte[] { 1, 2, 3 };
        var job = CreateJob(PriceSearchJobStatus.Queued, DateTimeOffset.UtcNow) with
        {
            Image = originalImage
        };
        var store = new InMemoryPriceJobStore();
        await store.AddAsync(job);
        originalImage[0] = 99;

        var result = await store.GetAsync(job.Id);

        Assert.NotNull(result);
        Assert.Equal(1, result.Image.Span[0]);
    }

    private static PriceSearchJob CreateJob(
        PriceSearchJobStatus status,
        DateTimeOffset createdAt) =>
        new(
            Guid.NewGuid(),
            new CarIdentificationResult("Twin Mill", 2024, "Hot Wheels", "Twin Mill"),
            new byte[] { 1, 2, 3 },
            "image/jpeg",
            status,
            null,
            null,
            createdAt,
            null,
            null);
}

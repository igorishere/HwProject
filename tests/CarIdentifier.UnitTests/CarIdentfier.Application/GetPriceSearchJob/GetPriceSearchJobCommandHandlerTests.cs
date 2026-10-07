using CarIdentifier.Application.Abstraction.PriceFinder;
using CarIdentifier.Application.CommandHandlers.GetPriceSearchJob;
using CarIdentifier.Application.Contracts;
using NSubstitute;

namespace CarIdentifier.UnitTests.CarIdentfier.Application.GetPriceSearchJob;

public sealed class GetPriceSearchJobCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnCarResult_WhenJobExists()
    {
        var jobId = Guid.NewGuid();
        var startedAt = DateTimeOffset.UtcNow.AddSeconds(-4);
        var job = new PriceSearchJob(
            jobId,
            new CarIdentificationResult("Twin Mill", 2024, "Hot Wheels", "Twin Mill", jobId),
            new byte[] { 1, 2, 3 },
            "image/jpeg",
            PriceSearchJobStatus.Running,
            null,
            null,
            DateTimeOffset.UtcNow.AddSeconds(-5),
            startedAt,
            null);
        var store = Substitute.For<IPriceJobStore>();
        store.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);
        var handler = new GetPriceSearchJobCommandHandler(store);

        var result = await handler.HandleAsync(new GetPriceSearchJobCommand(jobId));

        Assert.NotNull(result);
        Assert.Equal(job.CarIdentification, result.CarIdentification);
        Assert.Equal(PriceSearchJobStatus.Running, result.PriceSearchStatus);
        Assert.Equal(startedAt, result.PriceSearchStartedAt);
        Assert.Null(result.PriceSearchResult);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenJobDoesNotExist()
    {
        var store = Substitute.For<IPriceJobStore>();
        var handler = new GetPriceSearchJobCommandHandler(store);

        var result = await handler.HandleAsync(new GetPriceSearchJobCommand(Guid.NewGuid()));

        Assert.Null(result);
    }
}

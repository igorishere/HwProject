using CarIdentifier.Application.Abstraction.CarIdentification;
using CarIdentifier.Application.Abstraction.PriceFinder;
using CarIdentifier.Application.Contracts;
using NSubstitute;

namespace CarIdentifier.Application.CommandHandlers.IdentifyCar;

public sealed class IdentifyCarCommandHandlerTests
{
        private readonly IdentifyCarCommandHandler _handler;
        private readonly ICarIdentifier _carIdentifierMock;
        private readonly IPriceJobStore _priceJobStoreMock;
        private readonly IPriceJobQueue _priceJobQueueMock;

        public IdentifyCarCommandHandlerTests()
        {
                _carIdentifierMock = Substitute.For<ICarIdentifier>();
                _priceJobStoreMock = Substitute.For<IPriceJobStore>();
                _priceJobQueueMock = Substitute.For<IPriceJobQueue>();
                _handler = new IdentifyCarCommandHandler(
                        _carIdentifierMock,
                        _priceJobStoreMock,
                        _priceJobQueueMock);
        }

        [Fact]
        public async Task HandleAsync_ShouldReturnCarResult_WhenCarIsIdentified()
        {
                // Arrange
                _carIdentifierMock.IdentifyAsync(
                        Arg.Any<Stream>(),
                        Arg.Any<string>(),
                        Arg.Any<CancellationToken>())
                    .Returns(new CarIdentificationResult("Corolla", 2020, "Toyota", "Corolla", Guid.NewGuid()));
                PriceSearchJob? createdJob = null;
                _priceJobStoreMock.AddAsync(
                        Arg.Do<PriceSearchJob>(job => createdJob = job),
                        Arg.Any<CancellationToken>())
                    .Returns(Task.CompletedTask);
                Guid? queuedJobId = null;
                _priceJobQueueMock.EnqueueAsync(
                        Arg.Do<Guid>(id => queuedJobId = id),
                        Arg.Any<CancellationToken>())
                    .Returns(ValueTask.CompletedTask);

                var stream = new MemoryStream(new byte[] { 1, 2, 3 });

                var command = new IdentifyCarCommand(stream, "image/jpeg");

                // Act
                var result = await _handler.HandleAsync(command);

                // Assert
                Assert.NotNull(result);
                Assert.IsType<CarResult>(result);
                var resultIdentification = Assert.IsType<CarIdentificationResult>(result.CarIdentification);
                Assert.Equal("Corolla", resultIdentification.Name);
                Assert.NotEqual(Guid.Empty, resultIdentification.PriceSearchJobId);
                Assert.Null(result.PriceSearchResult);
                Assert.Equal(PriceSearchJobStatus.Queued, result.PriceSearchStatus);
                Assert.NotNull(createdJob);
                Assert.Equal(resultIdentification.PriceSearchJobId, createdJob.Id);
                Assert.Equal(PriceSearchJobStatus.Queued, createdJob.Status);
                Assert.Equal(new byte[] { 1, 2, 3 }, createdJob.Image.ToArray());
                Assert.Equal("image/jpeg", createdJob.MediaType);
                Assert.Equal("Corolla", createdJob.CarIdentification.Name);
                Assert.Equal(createdJob.Id, queuedJobId);
                await _priceJobStoreMock.Received(1).AddAsync(
                        Arg.Any<PriceSearchJob>(),
                        Arg.Any<CancellationToken>());
                await _priceJobQueueMock.Received(1).EnqueueAsync(
                        Arg.Any<Guid>(),
                        Arg.Any<CancellationToken>());
        }
}
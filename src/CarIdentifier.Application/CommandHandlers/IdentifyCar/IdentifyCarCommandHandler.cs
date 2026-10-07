using CarIdentifier.Application.Abstraction.CarIdentification;
using CarIdentifier.Application.Abstraction.PriceFinder;
using CarIdentifier.Application.Contracts;

namespace CarIdentifier.Application.CommandHandlers.IdentifyCar;

public sealed class IdentifyCarCommandHandler(
    ICarIdentifier carIdentifier,
    IPriceJobStore priceJobStore,
    IPriceJobQueue priceJobQueue)
    : ICommandHandler<IdentifyCarCommand, CarResult>
{
    public async Task<CarResult> HandleAsync(
        IdentifyCarCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var imageBuffer = new MemoryStream();
        await command.Image.CopyToAsync(imageBuffer, cancellationToken);
        var imageBytes = imageBuffer.ToArray();

        await using var identificationImage = new MemoryStream(imageBytes, writable: false);
        CarIdentificationResult carIdentification = await carIdentifier.IdentifyAsync(
            identificationImage,
            command.MediaType,
            cancellationToken);

        var priceSearchJobId = Guid.NewGuid();
        carIdentification = carIdentification with { PriceSearchJobId = priceSearchJobId };

        var now = DateTimeOffset.UtcNow;
        var priceSearchJob = new PriceSearchJob(
            priceSearchJobId,
            carIdentification,
            imageBytes,
            command.MediaType,
            PriceSearchJobStatus.Queued,
            null,
            null,
            now,
            null,
            null);

        await priceJobStore.AddAsync(priceSearchJob, CancellationToken.None);
        await priceJobQueue.EnqueueAsync(priceSearchJobId, CancellationToken.None);

        return new CarResult(
            carIdentification,
            PriceSearchStatus: PriceSearchJobStatus.Queued);
    }
}

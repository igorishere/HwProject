using CarIdentifier.Application.Contracts;

namespace CarIdentifier.Application.Abstraction.CarIdentification;

public interface ICarIdentifier
{
    Task<CarIdentificationResult> IdentifyAsync(
        Stream image,
        string mediaType,
        CancellationToken cancellationToken = default
    );
}

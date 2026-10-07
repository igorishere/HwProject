using CarIdentifier.Application.Abstraction.PriceFinder;
using CarIdentifier.Application.Contracts;

namespace CarIdentifier.Application.CommandHandlers.GetPriceSearchJob;

public sealed class GetPriceSearchJobCommandHandler(IPriceJobStore priceJobStore)
    : ICommandHandler<GetPriceSearchJobCommand, CarResult?>
{
    public async Task<CarResult?> HandleAsync(
        GetPriceSearchJobCommand command,
        CancellationToken cancellationToken = default)
    {
        var job = await priceJobStore.GetAsync(command.Id, cancellationToken);

        return job is null
            ? null
            : new CarResult(
                job.CarIdentification,
                job.Result,
                job.Status,
                job.StartedAt,
                job.CompletedAt,
                job.FailureReason);
    }
}

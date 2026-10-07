using CarIdentifier.Application;
using CarIdentifier.Application.CommandHandlers.GetPriceSearchJob;
using CarIdentifier.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CarIdentifier.Api.Controllers.GetPriceSearchJob;

[ApiController]
[Route("api/cars/price-jobs")]
public sealed class GetPriceSearchJobController(
    ICommandHandler<GetPriceSearchJobCommand, CarResult?> getPriceSearchJobCommandHandler)
    : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetPriceSearchJob")]
    public async Task<ActionResult<CarResult>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await getPriceSearchJobCommandHandler.HandleAsync(
            new GetPriceSearchJobCommand(id),
            cancellationToken);

        return result is null
            ? NotFound()
            : Ok(result);
    }
}

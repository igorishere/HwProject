using CarIdentifier.Application;
using CarIdentifier.Application.Abstraction.CarIdentification;
using CarIdentifier.Application.CommandHandlers.IdentifyCar;
using CarIdentifier.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CarIdentifier.Api.Controllers.IdentifyCar;

[ApiController]
[Route("api/cars/identify")]
public sealed class IdentifyCarController(
    ICommandHandler<IdentifyCarCommand, CarResult> identifyCarCommandHandler
) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CarResult>> Identify([FromForm] IdentifyCarRequest request, CancellationToken cancellationToken)
    {
        if (request.Image is null || request.Image.Length == 0)
        {
            return BadRequest(new { error = "An image file is required." });
        }

        await using var stream = request.Image.OpenReadStream();
        var mediaType = string.IsNullOrWhiteSpace(request.Image.ContentType)
            ? "application/octet-stream"
            : request.Image.ContentType;

        CarResult car = await identifyCarCommandHandler
            .HandleAsync(new IdentifyCarCommand(stream, mediaType), cancellationToken);

        return Ok(car);
    }
}

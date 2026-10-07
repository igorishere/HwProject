using Microsoft.AspNetCore.Mvc;

namespace CarIdentifier.Api.Controllers.HealthCheck;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthCheckController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new HealthCheckResponse("Healthy", DateTime.UtcNow));
}
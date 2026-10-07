namespace CarIdentifier.Api.Controllers.HealthCheck;

public sealed record HealthCheckResponse(string Status, DateTime Timestamp);

namespace CarIdentifier.Application.CommandHandlers.IdentifyCar;

public sealed record IdentifyCarCommand(Stream Image, string MediaType);

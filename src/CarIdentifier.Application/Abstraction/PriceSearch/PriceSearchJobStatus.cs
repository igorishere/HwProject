namespace CarIdentifier.Application.Abstraction.PriceFinder;

public enum PriceSearchJobStatus
{
    Pending,
    Queued,
    Running,
    Completed,
    Failed
}

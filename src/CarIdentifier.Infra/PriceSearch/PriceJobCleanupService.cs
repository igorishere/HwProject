using CarIdentifier.Application.Abstraction.PriceFinder;
using Microsoft.Extensions.Hosting;

namespace CarIdentifier.Infra.PriceSearch;

public sealed class PriceJobCleanupService(IPriceJobStore store) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CleanupInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await store.RemoveExpiredAsync(stoppingToken);
        }
    }
}

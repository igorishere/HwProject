using CarIdentifier.Application.Abstraction.PriceFinder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CarIdentifier.Infra.PriceSearch;

public sealed class PriceSearchWorker(
    IPriceJobQueue queue,
    IPriceJobStore store,
    IServiceScopeFactory scopeFactory,
    ILogger<PriceSearchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Guid jobId;
            try
            {
                jobId = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            var job = await store.GetAsync(jobId, stoppingToken);
            if (job is null)
            {
                logger.LogWarning("Price-search job {JobId} was not found in the store", jobId);
                continue;
            }

            var startedAt = DateTimeOffset.UtcNow;
            job = job with
            {
                Status = PriceSearchJobStatus.Running,
                StartedAt = startedAt
            };
            await store.UpdateAsync(job, stoppingToken);

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var priceSearcher = scope.ServiceProvider.GetRequiredService<IPriceSearcher>();
                await using var image = new MemoryStream(job.Image.ToArray(), writable: false);
                var result = await priceSearcher.FindPriceAsync(
                    new PriceSearchRequest(
                        job.CarIdentification.Name,
                        job.CarIdentification.YearOfRelease,
                        job.CarIdentification.Manufacturer,
                        job.CarIdentification.Model,
                        new ImageData(image, job.MediaType)),
                    stoppingToken);

                await store.UpdateAsync(
                    job with
                    {
                        Status = PriceSearchJobStatus.Completed,
                        Result = result,
                        CompletedAt = DateTimeOffset.UtcNow
                    },
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Price search failed for job {JobId}", jobId);
                await store.UpdateAsync(
                    job with
                    {
                        Status = PriceSearchJobStatus.Failed,
                        FailureReason = "A busca de preços falhou.",
                        CompletedAt = DateTimeOffset.UtcNow
                    },
                    stoppingToken);
            }
        }
    }
}

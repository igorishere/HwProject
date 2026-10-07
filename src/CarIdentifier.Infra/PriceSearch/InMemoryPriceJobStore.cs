using CarIdentifier.Application.Abstraction.PriceFinder;
using System.Collections.Concurrent;

namespace CarIdentifier.Infra.PriceSearch;

public sealed class InMemoryPriceJobStore : IPriceJobStore
{
    private static readonly TimeSpan PendingJobLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan FinishedJobLifetime = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<Guid, PriceSearchJob> _jobs = new();

    public Task AddAsync(PriceSearchJob job, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_jobs.TryAdd(job.Id, CopyImage(job)))
        {
            throw new InvalidOperationException($"Price-search job '{job.Id}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<PriceSearchJob?> GetAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_jobs.TryGetValue(jobId, out var job))
        {
            return Task.FromResult<PriceSearchJob?>(null);
        }

        if (IsExpired(job, DateTimeOffset.UtcNow))
        {
            _jobs.TryRemove(new KeyValuePair<Guid, PriceSearchJob>(jobId, job));
            return Task.FromResult<PriceSearchJob?>(null);
        }

        return Task.FromResult<PriceSearchJob?>(job);
    }

    public Task UpdateAsync(PriceSearchJob job, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var updatedJob = CopyImage(job);
        while (_jobs.TryGetValue(job.Id, out var currentJob))
        {
            if (_jobs.TryUpdate(job.Id, updatedJob, currentJob))
            {
                return Task.CompletedTask;
            }
        }

        throw new KeyNotFoundException($"Price-search job '{job.Id}' was not found.");
    }

    public Task RemoveExpiredAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;

        foreach (var (jobId, job) in _jobs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsExpired(job, now))
            {
                _jobs.TryRemove(new KeyValuePair<Guid, PriceSearchJob>(jobId, job));
            }
        }

        return Task.CompletedTask;
    }

    private static bool IsExpired(PriceSearchJob job, DateTimeOffset now) =>
        job.Status switch
        {
            PriceSearchJobStatus.Pending =>
                now - job.CreatedAt >= PendingJobLifetime,
            PriceSearchJobStatus.Completed or PriceSearchJobStatus.Failed =>
                job.CompletedAt is { } completedAt && now - completedAt >= FinishedJobLifetime,
            _ => false
        };

    private static PriceSearchJob CopyImage(PriceSearchJob job) =>
        job with { Image = job.Image.ToArray() };
}

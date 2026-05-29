using LawtonJobBoardsServices.Models.Dto;
using LawtonJobBoardsServices.Models.Ordant;
using LawtonJobBoardsServices.Services.Interfaces;
using LawtonJobBoardsServices.Utilities;

namespace LawtonJobBoardsServices.Services;

public record ProcessResult(
    bool Success,
    IReadOnlyDictionary<int, OrdantJobsSnapShot> UpdatedCache,
    bool IsInitialized);

public class JobChangeProcessor(
    IJobBroadcaster broadcaster,
    JobChangeDiffLogger diffLogger,
    IDueStatusCalculator calculator,
    ILogger<JobChangeProcessor> logger)
{
    public async Task<ProcessResult> ProcessAsync(
        IReadOnlyList<OrdantSchedulerJob> freshJobs,
        IReadOnlyDictionary<int, OrdantJobsSnapShot> currentCache,
        bool isInitialized,
        CancellationToken ct)
    {
        // Build an updated cache as a new dictionary — never mutate the input.
        // Only committed to the caller when Success = true (atomicity).
        var updatedCache = new Dictionary<int, OrdantJobsSnapShot>(currentCache);

        foreach (var job in freshJobs)
        {
            var snapshot = OrdantSnapshotMapper.Map(job);
            var dto = ResourcePlannerMapper.MapJob(job, calculator);

            if (currentCache.TryGetValue(job.Id, out var cached))
            {
                if (cached == snapshot) continue;

                var diffs = SnapshotDiffer.Diff(cached, snapshot);
                var itemDescription = job.JobQueue?.OrderItem?.Description;
                diffLogger.Log(new JobChangeDiffEntry(DateTimeOffset.UtcNow, job.Id, itemDescription, diffs));
                logger.LogInformation(
                    "[DIFF] Job {JobId} ({Item}): {Props}",
                    job.Id, itemDescription, string.Join(", ", diffs.Select(d => d.Property)));

                try
                {
                    await broadcaster.BroadcastJobUpdateAsync(dto, ct);
                    updatedCache[job.Id] = snapshot;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to broadcast update for job {JobId}.", job.Id);
                    return new ProcessResult(false, currentCache, isInitialized);
                }
            }
            else if (!isInitialized)
            {
                // First run: populate cache silently, no broadcast.
                updatedCache[job.Id] = snapshot;
            }
            else
            {
                // New job appeared after initialisation.
                try
                {
                    await broadcaster.BroadcastJobUpdateAsync(dto, ct);
                    updatedCache[job.Id] = snapshot;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to broadcast new job {JobId}.", job.Id);
                    return new ProcessResult(false, currentCache, isInitialized);
                }
            }
        }

        // Detect jobs that vanished (deleted or moved to an untracked station).
        if (isInitialized)
        {
            var freshIds = new HashSet<int>(freshJobs.Select(j => j.Id));
            var removedIds = currentCache.Keys.Where(id => !freshIds.Contains(id)).ToList();

            foreach (var removedId in removedIds)
            {
                try
                {
                    await broadcaster.BroadcastJobRemovedAsync(removedId, ct);
                    updatedCache.Remove(removedId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to broadcast removal for job {JobId}.", removedId);
                    return new ProcessResult(false, currentCache, isInitialized);
                }
            }
        }

        // IsInitialized = true on every success, including empty-job-list polls (Bug 1 fix).
        return new ProcessResult(true, updatedCache, IsInitialized: true);
    }
}

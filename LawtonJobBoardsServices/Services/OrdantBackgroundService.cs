using LawtonJobBoardsServices.Models.Ordant;
using LawtonJobBoardsServices.Services.Interfaces;

namespace LawtonJobBoardsServices.Services;

public class OrdantBackgroundService(
    IJobBroadcaster broadcaster,
    IServiceScopeFactory scopeFactory,
    JobChangeProcessor processor,
    ILogger<OrdantBackgroundService> logger) : BackgroundService
{
    private IReadOnlyDictionary<int, OrdantJobsSnapShot> _cache = new Dictionary<int, OrdantJobsSnapShot>();
    private bool _isInitialized;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await broadcaster.EnsureConnectedAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not connect to SignalR hub; retrying in 10s.");
                await Delay(stoppingToken);
                continue;
            }

            IReadOnlyList<OrdantSchedulerJob> jobs;
            try
            {
                using var scope = scopeFactory.CreateScope();
                var poller = scope.ServiceProvider.GetRequiredService<IJobPoller>();
                jobs = await poller.GetAllJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch jobs from Ordant; retrying in 10s.");
                await Delay(stoppingToken);
                continue;
            }

            try
            {
                var result = await processor.ProcessAsync(jobs, _cache, _isInitialized, stoppingToken);
                if (result.Success)
                {
                    _cache = result.UpdatedCache;
                    _isInitialized = result.IsInitialized;
                }
                else
                {
                    logger.LogWarning("Broadcast failure; cache not updated. Will retry next tick.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during job processing.");
            }

            await Delay(stoppingToken);
        }
    }

    private static Task Delay(CancellationToken ct) =>
        Task.Delay(TimeSpan.FromSeconds(10), ct);
}

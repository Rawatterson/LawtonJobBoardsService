using LawtonJobBoardsServices.Models.Dto;

namespace LawtonJobBoardsServices.Services.Interfaces;

public interface IJobBroadcaster
{
    bool IsConnected { get; }
    Task EnsureConnectedAsync(CancellationToken ct);
    Task BroadcastJobUpdateAsync(ResourcePlannerJobDto job, CancellationToken ct);
    Task BroadcastJobRemovedAsync(int jobId, CancellationToken ct);
}

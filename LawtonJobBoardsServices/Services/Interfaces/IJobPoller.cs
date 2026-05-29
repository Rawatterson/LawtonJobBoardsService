using LawtonJobBoardsServices.Models.Ordant;

namespace LawtonJobBoardsServices.Services.Interfaces;

public interface IJobPoller
{
    Task<IReadOnlyList<OrdantSchedulerJob>> GetAllJobsAsync(CancellationToken ct);
}

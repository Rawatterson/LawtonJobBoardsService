using LawtonJobBoardsServices.Models.Ordant;

namespace LawtonJobBoardsServices.Services.Interfaces;

public interface IOrdantClient
{
    Task<OrdantPagedResponse<OrdantOrderSummary>?> GetOrdersAsync(
        int page = 1, int limitPerPage = 100, bool? isComplete = null,
        string? statusFilter = null, CancellationToken ct = default);

    Task<OrdantOrderDetail?> GetOrderAsync(int orderId, CancellationToken ct = default);

    Task<OrdantPagedResponse<OrdantSchedulerJob>?> GetSchedulerJobsAsync(
        int page = 1, int limitPerPage = 100, bool? isComplete = null,
        int? stationId = null, bool? hasCompletedDependencies = null,
        CancellationToken ct = default);

    Task<OrdantSchedulerJob?> GetSchedulerJobAsync(int jobId, CancellationToken ct = default);

    Task<OrdantPagedResponse<OrdantOrderItemSummary>?> GetOrderItemsAsync(
        int page = 1, int limitPerPage = 100, bool? isComplete = null,
        int? orderId = null, CancellationToken ct = default);

    Task<OrdantOrderItemDetail?> GetOrderItemAsync(int itemId, CancellationToken ct = default);
}

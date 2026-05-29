using LawtonJobBoardsServices.Models.Ordant;

namespace LawtonJobBoardsServices.Utilities;

public static class OrdantSnapshotMapper
{
    public static OrdantJobsSnapShot Map(OrdantSchedulerJob job)
    {
        var orderItem = job.JobQueue?.OrderItem;
        var order = orderItem?.Order;

        return new OrdantJobsSnapShot
        {
            Id = job.Id,
            Name = job.Name,
            IsComplete = job.IsComplete,
            Progress = job.Progress,
            Timer = job.Timer,
            TimeEstimated = job.TimeEstimated,

            OrdantStationId = job.Station?.Id ?? 0,
            StationName = job.Station?.Name,
            StationGroupId = job.Station?.Group?.Id,
            StationGroupName = job.Station?.Group?.Name,

            SortOrder = orderItem?.SortId,

            OrderId = order?.Id,
            OrderInternalId = order?.InternalId,
            OrderProjectName = order?.ProjectName,
            OrderStatusLabel = order?.Status?.Value,
            OrderPriorityLabel = order?.Priority?.Label,
            OrderCustomerName = order?.Customer?.FullName,
            OrderDueDate = order?.DueDate,
            OrderProgress = order?.Progress,

            OrderItemId = orderItem?.Id,
            OrderItemDescription = orderItem?.Description,
            OrderItemSku = orderItem?.Sku,
            OrderItemSortId = orderItem?.SortId,
            OrderItemQty = orderItem?.Qty,
            OrderItemIsComplete = orderItem?.IsComplete,
            OrderItemProgress = orderItem?.Progress,
            OrderItemDateDue = orderItem?.DateDue,
            OrderItemDateShipBy = orderItem?.DateShipBy,
            OrderItemDateProofDue = orderItem?.DateProofDue,
        };
    }
}

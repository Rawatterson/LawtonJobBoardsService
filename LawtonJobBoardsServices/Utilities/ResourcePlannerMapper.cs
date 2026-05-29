using LawtonJobBoardsServices.Models.Dto;
using LawtonJobBoardsServices.Models.Ordant;
using LawtonJobBoardsServices.Services.Interfaces;

namespace LawtonJobBoardsServices.Utilities;

public static class ResourcePlannerMapper
{
    public static ResourcePlannerJobDto MapJob(OrdantSchedulerJob j, IDueStatusCalculator dueStatus)
    {
        var orderItem = j.JobQueue?.OrderItem;
        var order = orderItem?.Order;

        // Use the order-item-level due date for DueStatus; OrdantOrderRef inside a
        // scheduler job response does not carry the order-level dueDate field.
        var dueDateForStatus = orderItem?.DateDue;

        return new ResourcePlannerJobDto
        {
            Id = j.Id,
            Name = j.Name,
            IsComplete = j.IsComplete,
            Progress = j.Progress,
            StationId = j.Station?.Id,
            StationName = j.Station?.Name,
            StationGroupId = j.Station?.Group?.Id,
            StationGroupName = j.Station?.Group?.Name,
            StationSortOrder = j.StationSortOrder,
            SortOrder = j.SortOrder,
            HasCompletedDependencies = j.HasCompletedDependencies,
            TimeEstimated = j.TimeEstimated,
            TimeActual = j.TimeActual,
            Timer = j.Timer,
            OrderId = order?.Id,
            OrderInternalId = order?.InternalId,
            OrderProjectName = order?.ProjectName,
            OrderStatusLabel = order?.Status?.Value,
            OrderPriorityLabel = order?.Priority?.Label,
            OrderCustomerName = order?.Customer?.FullName,
            OrderCompanyName = order?.Customer?.Company?.Name ?? order?.Customer?.Name,
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
            DueStatus = dueStatus.Calculate(dueDateForStatus, j.IsComplete),
            ResourceTypes = j.Resources
                .Select(r => r.ResourceType)
                .Where(rt => rt is not null)
                .Select(rt => rt!)
                .Distinct()
                .ToList(),
        };
    }
}
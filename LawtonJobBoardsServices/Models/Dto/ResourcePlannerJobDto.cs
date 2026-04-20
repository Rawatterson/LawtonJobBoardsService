namespace LawtonJobBoardsServices.Models.Dto;

public class ResourcePlannerJobDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
    public string? Progress { get; set; }
    public int? StationId { get; set; }
    public string? StationName { get; set; }
    public int StationSortOrder { get; set; }
    public int SortOrder { get; set; }
    public bool HasCompletedDependencies { get; set; }
    public string? TimeEstimated { get; set; }
    public string? TimeActual { get; set; }
    public bool Timer { get; set; }

    // Derived from jobQueue -> orderItem -> order
    public int? OrderId { get; set; }
    public string? OrderInternalId { get; set; }
    public string? OrderProjectName { get; set; }
    public string? OrderStatusLabel { get; set; }
    public string? OrderPriorityLabel { get; set; }
    public string? OrderCustomerName { get; set; }
    public string? OrderCompanyName { get; set; }
    public DateTimeOffset? OrderDueDate { get; set; }

    // From orderItem directly (may be more specific than order-level due date)
    public int? OrderItemId { get; set; }
    public string? OrderItemDescription { get; set; }
    public DateTimeOffset? OrderItemDateDue { get; set; }
    public DateTimeOffset? OrderItemDateShipBy { get; set; }

    public DueStatus DueStatus { get; set; }
    public List<string> ResourceTypes { get; set; } = [];
}

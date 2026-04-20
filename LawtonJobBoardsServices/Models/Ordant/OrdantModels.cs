using System.Text.Json.Serialization;

namespace LawtonJobBoardsServices.Models.Ordant;

// ── Shared ────────────────────────────────────────────────────────────────────

public class OrdantPagedResponse<T>
{
    public int Count { get; set; }
    public int LimitPerPage { get; set; }
    public int Offset { get; set; }
    public List<T> ResultSet { get; set; } = [];
}

public class OrdantStatus
{
    public int Id { get; set; }
    public string? Value { get; set; }
}

public class OrdantPriority
{
    public int Id { get; set; }
    public string? Label { get; set; }
    public int? Value { get; set; }
}

public class OrdantPersonRef
{
    public int Id { get; set; }
    public string? FullName { get; set; }
}

public class OrdantCompanyRef
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
}

public class OrdantCustomer
{
    public int Id { get; set; }
    public string? Type { get; set; }
    public string? FullName { get; set; }
    /// <summary>Company name when customer type is "company".</summary>
    public string? Name { get; set; }
    public OrdantCompanyRef? Company { get; set; }
}

public class OrdantFinancialSummary
{
    public string Total { get; set; } = "0.00";
    public string Subtotal { get; set; } = "0.00";
}

// ── Orders ────────────────────────────────────────────────────────────────────

/// <summary>Shape returned by GET /order (list view via serializer).</summary>
public class OrdantOrderSummary
{
    public int Id { get; set; }
    public string InternalId { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public bool IsComplete { get; set; }
    public string? Progress { get; set; }
    public OrdantStatus? Status { get; set; }
    public OrdantPriority? Priority { get; set; }
    public OrdantCustomer? Customer { get; set; }
    public OrdantFinancialSummary? Summary { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Shape returned by GET /order/{orderId} (full detail).</summary>
public class OrdantOrderDetail : OrdantOrderSummary
{
    public string? NotesCustomer { get; set; }
    public string? NotesInternal { get; set; }
    public string? PoNumber { get; set; }
    public string? CustomerOrderId { get; set; }
    public string? TransactionId { get; set; }
    public OrdantPersonRef? AssignedTo { get; set; }
    public OrdantPersonRef? AccountManager { get; set; }
    public OrdantPersonRef? SalesPerson { get; set; }
    public List<OrdantOrderItem> Items { get; set; } = [];
}

public class OrdantOrderItem
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public int SortOrder { get; set; }
    public string? SortId { get; set; }
    public DateTimeOffset? DateDue { get; set; }
    public DateTimeOffset? DateShipBy { get; set; }
    public DateTimeOffset? DateProofDue { get; set; }
}

// ── Scheduler / Resource Planner ──────────────────────────────────────────────

/// <summary>Shape returned by GET /scheduler/job (list).</summary>
public class OrdantSchedulerJob
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
    public string? Progress { get; set; }
    public OrdantStation? Station { get; set; }
    public int StationSortOrder { get; set; }
    public int SortOrder { get; set; }
    public bool HasCompletedDependencies { get; set; }
    public bool Timer { get; set; }
    public string? TimeActual { get; set; }
    public string? TimeEstimated { get; set; }
    public OrdantJobQueue? JobQueue { get; set; }
    public List<OrdantResource> Resources { get; set; } = [];
}

public class OrdantStation
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class OrdantJobQueue
{
    public int Id { get; set; }
    public bool IsComplete { get; set; }
    public OrdantOrderItemRef? OrderItem { get; set; }
}

public class OrdantOrderItemRef
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? DateDue { get; set; }
    public DateTimeOffset? DateProofDue { get; set; }
    public DateTimeOffset? DateShipBy { get; set; }
    public string? SortId { get; set; }
    public int SortOrder { get; set; }
    public OrdantOrderRef? Order { get; set; }
}

/// <summary>Minimal order context embedded inside a scheduler job or order item.</summary>
public class OrdantOrderRef
{
    public int Id { get; set; }
    public string? InternalId { get; set; }
    public string? ProjectName { get; set; }
    /// <summary>Order type (e.g. "estimate", "order") — present in order-item responses.</summary>
    public string? Type { get; set; }
    public OrdantStatus? Status { get; set; }
    public OrdantPriority? Priority { get; set; }
    public OrdantCustomer? Customer { get; set; }
}

public class OrdantResource
{
    public int Id { get; set; }
    public bool Availability { get; set; }
    public string? ResourceType { get; set; }
}

// ── Order Items ───────────────────────────────────────────────────────────────

/// <summary>Pricing summary embedded in an order-item response.</summary>
public class OrdantItemPricing
{
    public string? SellPrice { get; set; }
    public string? SellPriceExtended { get; set; }
    public string? Cost { get; set; }
    public string? UnitCost { get; set; }
}

/// <summary>Shape returned by GET /order-item (list with serializer).</summary>
public class OrdantOrderItemSummary
{
    public int Id { get; set; }
    public string? Sku { get; set; }
    public string? SortId { get; set; }
    public int SortOrder { get; set; }
    public int Qty { get; set; }
    public DateTimeOffset? DateDue { get; set; }
    public DateTimeOffset? DateShipBy { get; set; }
    public DateTimeOffset? DateProofDue { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset? DateLastUpdated { get; set; }
    public OrdantOrderRef? Order { get; set; }
}

/// <summary>Shape returned by GET /order-item/{id} (full detail).</summary>
public class OrdantOrderItemDetail : OrdantOrderItemSummary
{
    public string? Description { get; set; }
    public bool IsComplete { get; set; }
    public bool IsDetached { get; set; }
    public string? Progress { get; set; }
    public OrdantItemPricing? Summary { get; set; }
}

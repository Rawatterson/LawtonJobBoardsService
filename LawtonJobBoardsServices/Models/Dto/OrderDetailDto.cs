namespace LawtonJobBoardsServices.Models.Dto;

public class OrderDetailDto : OrderSummaryDto
{
    public string? NotesCustomer { get; set; }
    public string? NotesInternal { get; set; }
    public string? PoNumber { get; set; }
    public string? CustomerOrderId { get; set; }
    public string? TransactionId { get; set; }
    public string? AssignedToName { get; set; }
    public string? AccountManagerName { get; set; }
    public string? SalesPersonName { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}

public class OrderItemDto
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

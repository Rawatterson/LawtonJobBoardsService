namespace LawtonJobBoardsServices.Models.Dto;

public class OrderItemSummaryDto
{
    public int Id { get; set; }
    public string? Sku { get; set; }
    public string? SortId { get; set; }
    public int Qty { get; set; }
    public DateTimeOffset? DateDue { get; set; }
    public DateTimeOffset? DateShipBy { get; set; }
    public DateTimeOffset? DateProofDue { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public int? OrderId { get; set; }
    public string? OrderInternalId { get; set; }
    public string? OrderProjectName { get; set; }
    public string? OrderType { get; set; }
    public DueStatus DueStatus { get; set; }
}

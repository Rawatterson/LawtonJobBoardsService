namespace LawtonJobBoardsServices.Models.Dto;

public class OrderItemDetailDto : OrderItemSummaryDto
{
    public string? Description { get; set; }
    public bool IsComplete { get; set; }
    public bool IsDetached { get; set; }
    public string? Progress { get; set; }
    public DateTimeOffset? DateLastUpdated { get; set; }
    public string? OrderStatusLabel { get; set; }
    public decimal SellPrice { get; set; }
    public decimal Cost { get; set; }
}

namespace LawtonJobBoardsServices.Models.Dto;

public class OrderSummaryDto
{
    public int Id { get; set; }
    public string InternalId { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public bool IsComplete { get; set; }
    public string? Progress { get; set; }
    public string? StatusLabel { get; set; }
    public string? PriorityLabel { get; set; }
    public int? PriorityValue { get; set; }
    /// <summary>Contact full name (when customer type is "contact").</summary>
    public string? CustomerName { get; set; }
    /// <summary>Company name associated with the order.</summary>
    public string? CompanyName { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public decimal Total { get; set; }
    public DueStatus DueStatus { get; set; }
}

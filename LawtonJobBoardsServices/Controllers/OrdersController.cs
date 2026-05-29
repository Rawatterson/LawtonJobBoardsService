using LawtonJobBoardsServices.Models.Dto;
using LawtonJobBoardsServices.Models.Ordant;
using LawtonJobBoardsServices.Services;
using LawtonJobBoardsServices.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LawtonJobBoardsServices.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrdantClient ordant, IDueStatusCalculator dueStatus) : ControllerBase
{
    /// <summary>
    /// Returns a paged list of orders (jobs) ordered by due date ascending.
    /// </summary>
    /// <param name="page">1-based page number (default 1).</param>
    /// <param name="limitPerPage">Results per page (default 100).</param>
    /// <param name="isComplete">Filter by completion state. Omit to return all.</param>
    /// <param name="status">Filter by status label substring (e.g. "Production").</param>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<OrderSummaryDto>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int limitPerPage = 100,
        [FromQuery] bool? isComplete = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await ordant.GetOrdersAsync(page, limitPerPage, isComplete, status, ct);
        if (result is null)
            return Ok(new PagedResultDto<OrderSummaryDto>());

        return Ok(new PagedResultDto<OrderSummaryDto>
        {
            Count = result.Count,
            LimitPerPage = result.LimitPerPage,
            Offset = result.Offset,
            ResultSet = result.ResultSet.Select(MapSummary).ToList(),
        });
    }

    /// <summary>
    /// Returns the full detail for a single order.
    /// </summary>
    [HttpGet("{orderId:int}")]
    public async Task<ActionResult<OrderDetailDto>> GetOrder(int orderId, CancellationToken ct = default)
    {
        var order = await ordant.GetOrderAsync(orderId, ct);
        if (order is null)
            return NotFound();

        return Ok(MapDetail(order));
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private OrderSummaryDto MapSummary(OrdantOrderSummary o) => new()
    {
        Id = o.Id,
        InternalId = o.InternalId,
        ProjectName = o.ProjectName,
        DueDate = o.DueDate,
        IsComplete = o.IsComplete,
        Progress = o.Progress,
        StatusLabel = o.Status?.Value,
        PriorityLabel = o.Priority?.Label,
        PriorityValue = o.Priority?.Value,
        CustomerName = o.Customer?.FullName,
        CompanyName = o.Customer?.Company?.Name ?? o.Customer?.Name,
        CreationDate = o.CreationDate,
        Total = decimal.TryParse(o.Summary?.Total, out var t) ? t : 0m,
        DueStatus = dueStatus.Calculate(o.DueDate, o.IsComplete),
    };

    private OrderDetailDto MapDetail(OrdantOrderDetail o) => new()
    {
        Id = o.Id,
        InternalId = o.InternalId,
        ProjectName = o.ProjectName,
        DueDate = o.DueDate,
        IsComplete = o.IsComplete,
        Progress = o.Progress,
        StatusLabel = o.Status?.Value,
        PriorityLabel = o.Priority?.Label,
        PriorityValue = o.Priority?.Value,
        CustomerName = o.Customer?.FullName,
        CompanyName = o.Customer?.Company?.Name ?? o.Customer?.Name,
        CreationDate = o.CreationDate,
        Total = decimal.TryParse(o.Summary?.Total, out var t) ? t : 0m,
        DueStatus = dueStatus.Calculate(o.DueDate, o.IsComplete),
        NotesCustomer = o.NotesCustomer,
        NotesInternal = o.NotesInternal,
        PoNumber = o.PoNumber,
        CustomerOrderId = o.CustomerOrderId,
        TransactionId = o.TransactionId,
        AssignedToName = o.AssignedTo?.FullName,
        AccountManagerName = o.AccountManager?.FullName,
        SalesPersonName = o.SalesPerson?.FullName,
        Items = o.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            Description = i.Description,
            Sku = i.Sku,
            SortOrder = i.SortOrder,
            SortId = i.SortId,
            DateDue = i.DateDue,
            DateShipBy = i.DateShipBy,
            DateProofDue = i.DateProofDue,
        }).ToList(),
    };
}

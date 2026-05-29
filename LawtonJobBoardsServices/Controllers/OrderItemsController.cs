using LawtonJobBoardsServices.Models.Dto;
using LawtonJobBoardsServices.Models.Ordant;
using LawtonJobBoardsServices.Services;
using LawtonJobBoardsServices.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LawtonJobBoardsServices.Controllers;

[ApiController]
[Route("api/order-items")]
public class OrderItemsController(IOrdantClient ordant, IDueStatusCalculator dueStatus) : ControllerBase
{
    /// <summary>
    /// Returns a paged list of order items (jobs) ordered by due date ascending.
    /// </summary>
    /// <param name="page">1-based page number (default 1).</param>
    /// <param name="limitPerPage">Results per page (default 100).</param>
    /// <param name="isComplete">Filter by completion state. Omit to return all.</param>
    /// <param name="orderId">Filter to items belonging to a specific order.</param>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<OrderItemSummaryDto>>> GetOrderItems(
        [FromQuery] int page = 1,
        [FromQuery] int limitPerPage = 100,
        [FromQuery] bool? isComplete = null,
        [FromQuery] int? orderId = null,
        CancellationToken ct = default)
    {
        var result = await ordant.GetOrderItemsAsync(page, limitPerPage, isComplete, orderId, ct);
        if (result is null)
            return Ok(new PagedResultDto<OrderItemSummaryDto>());

        return Ok(new PagedResultDto<OrderItemSummaryDto>
        {
            Count = result.Count,
            LimitPerPage = result.LimitPerPage,
            Offset = result.Offset,
            ResultSet = result.ResultSet.Select(MapSummary).ToList(),
        });
    }

    /// <summary>
    /// Returns the full detail for a single order item.
    /// </summary>
    [HttpGet("{itemId:int}")]
    public async Task<ActionResult<OrderItemDetailDto>> GetOrderItem(int itemId, CancellationToken ct = default)
    {
        var item = await ordant.GetOrderItemAsync(itemId, ct);
        if (item is null)
            return NotFound();

        return Ok(MapDetail(item));
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private OrderItemSummaryDto MapSummary(OrdantOrderItemSummary i) => new()
    {
        Id = i.Id,
        Sku = i.Sku,
        SortId = i.SortId,
        Qty = i.Qty,
        DateDue = i.DateDue,
        DateShipBy = i.DateShipBy,
        DateProofDue = i.DateProofDue,
        CreationDate = i.CreationDate,
        OrderId = i.Order?.Id,
        OrderInternalId = i.Order?.InternalId,
        OrderProjectName = i.Order?.ProjectName,
        OrderType = i.Order?.Type,
        // isComplete is not available from the list endpoint; pass false so
        // DueStatus reflects the due date only — use isComplete=false filter
        // to exclude completed items from the list.
        DueStatus = dueStatus.Calculate(i.DateDue, isComplete: false),
    };

    private OrderItemDetailDto MapDetail(OrdantOrderItemDetail i) => new()
    {
        Id = i.Id,
        Sku = i.Sku,
        SortId = i.SortId,
        Qty = i.Qty,
        DateDue = i.DateDue,
        DateShipBy = i.DateShipBy,
        DateProofDue = i.DateProofDue,
        CreationDate = i.CreationDate,
        OrderId = i.Order?.Id,
        OrderInternalId = i.Order?.InternalId,
        OrderProjectName = i.Order?.ProjectName,
        OrderType = i.Order?.Type,
        DueStatus = dueStatus.Calculate(i.DateDue, i.IsComplete),
        Description = i.Description,
        IsComplete = i.IsComplete,
        IsDetached = i.IsDetached,
        Progress = i.Progress,
        DateLastUpdated = i.DateLastUpdated,
        OrderStatusLabel = i.Order?.Status?.Value,
        SellPrice = decimal.TryParse(i.Summary?.SellPrice, out var sell) ? sell : 0m,
        Cost = decimal.TryParse(i.Summary?.Cost, out var cost) ? cost : 0m,
    };
}

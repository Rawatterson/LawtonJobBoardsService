using LawtonJobBoardsServices.Models.Dto;
using LawtonJobBoardsServices.Models.Ordant;
using LawtonJobBoardsServices.Services;
using Microsoft.AspNetCore.Mvc;

namespace LawtonJobBoardsServices.Controllers;

[ApiController]
[Route("api/resource-planner")]
public class ResourcePlannerController(OrdantClient ordant, DueStatusCalculator dueStatus) : ControllerBase
{
    /// <summary>
    /// Returns scheduler jobs from the Ordant Resource Planner, sorted by
    /// station position then job sort order.
    /// </summary>
    /// <param name="page">1-based page number (default 1).</param>
    /// <param name="limitPerPage">Results per page (default 100).</param>
    /// <param name="isComplete">Filter by completion state. Omit to return all.</param>
    /// <param name="stationId">Filter to a specific production station ID.</param>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ResourcePlannerJobDto>>> GetJobs(
        [FromQuery] int page = 1,
        [FromQuery] int limitPerPage = 100,
        [FromQuery] bool? isComplete = null,
        [FromQuery] int? stationId = null,
        CancellationToken ct = default)
    {
        var result = await ordant.GetSchedulerJobsAsync(page, limitPerPage, isComplete, stationId, ct);
        if (result is null)
            return Ok(new PagedResultDto<ResourcePlannerJobDto>());

        return Ok(new PagedResultDto<ResourcePlannerJobDto>
        {
            Count = result.Count,
            LimitPerPage = result.LimitPerPage,
            Offset = result.Offset,
            ResultSet = result.ResultSet.Select(MapJob).ToList(),
        });
    }

    /// <summary>
    /// Returns the full detail for a single scheduler job.
    /// </summary>
    [HttpGet("{jobId:int}")]
    public async Task<ActionResult<ResourcePlannerJobDto>> GetJob(int jobId, CancellationToken ct = default)
    {
        var job = await ordant.GetSchedulerJobAsync(jobId, ct);
        if (job is null)
            return NotFound();

        return Ok(MapJob(job));
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private ResourcePlannerJobDto MapJob(OrdantSchedulerJob j)
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
            OrderItemId = orderItem?.Id,
            OrderItemDescription = orderItem?.Description,
            OrderItemDateDue = orderItem?.DateDue,
            OrderItemDateShipBy = orderItem?.DateShipBy,
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

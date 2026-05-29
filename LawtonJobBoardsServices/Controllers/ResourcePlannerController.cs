using LawtonJobBoardsServices.Models.Dto;
using LawtonJobBoardsServices.Models.Ordant;
using LawtonJobBoardsServices.Services;
using LawtonJobBoardsServices.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using LawtonJobBoardsServices.Utilities; 

namespace LawtonJobBoardsServices.Controllers;

[ApiController]
[Route("api/resource-planner")]
public class ResourcePlannerController(IOrdantClient ordant, IDueStatusCalculator dueStatusCalculator) : ControllerBase
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
        [FromQuery] bool? hasCompletedDependencies = null,
        CancellationToken ct = default)
    {
        var result = await ordant.GetSchedulerJobsAsync(page, limitPerPage, isComplete, stationId, hasCompletedDependencies, ct);
        if (result is null)
            return Ok(new PagedResultDto<ResourcePlannerJobDto>());

        return Ok(new PagedResultDto<ResourcePlannerJobDto>
        {
            Count = result.Count,
            LimitPerPage = result.LimitPerPage,
            Offset = result.Offset,
            ResultSet = result.ResultSet.Select(j => ResourcePlannerMapper.MapJob(j, dueStatusCalculator)).ToList(),
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
        return Ok(ResourcePlannerMapper.MapJob(job, dueStatusCalculator));
    }
}

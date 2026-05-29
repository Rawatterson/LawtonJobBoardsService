using LawtonJobBoardsServices.Models.Dto;
using LawtonJobBoardsServices.Services;
using Microsoft.AspNetCore.Mvc;

namespace LawtonJobBoardsServices.Controllers;

[ApiController]
[Route("api/debug")]
[ApiExplorerSettings(IgnoreApi = true)]
public class DebugController(JobChangeDiffLogger diffLogger) : ControllerBase
{
    [HttpGet("job-changes")]
    public IReadOnlyList<JobChangeDiffEntry> GetJobChanges([FromQuery] int limit = 100)
    {
        return diffLogger.GetRecent(limit);
    }
}

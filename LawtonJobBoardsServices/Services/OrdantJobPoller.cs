using LawtonJobBoardsServices.Configuration;
using LawtonJobBoardsServices.Models.Ordant;
using LawtonJobBoardsServices.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace LawtonJobBoardsServices.Services;

public class OrdantJobPoller(
    IOrdantClient client,
    IOptions<BoardStationsSettings> boardStations) : IJobPoller
{
    public async Task<IReadOnlyList<OrdantSchedulerJob>> GetAllJobsAsync(CancellationToken ct)
    {
        var allJobs = new List<OrdantSchedulerJob>();

        foreach (var stationId in boardStations.Value.StationIds)
        {
            var page = 1;
            var fetched = 0;
            var total = 0;

            do
            {
                var response = await client.GetSchedulerJobsAsync(page: page, stationId: stationId, ct: ct);
                if (response is null) break;
                allJobs.AddRange(response.ResultSet);
                fetched += response.ResultSet.Count;
                total = response.Count;
                page++;
            }
            while (fetched < total);
        }

        return allJobs;
    }
}

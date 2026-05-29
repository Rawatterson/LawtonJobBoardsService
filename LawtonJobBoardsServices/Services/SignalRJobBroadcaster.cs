using LawtonJobBoardsServices.Configuration;
using LawtonJobBoardsServices.Models.Dto;
using LawtonJobBoardsServices.Services.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace LawtonJobBoardsServices.Services;

public class SignalRJobBroadcaster(
    IOptions<JobBoardHubSettings> settings,
    ILogger<SignalRJobBroadcaster> logger) : IJobBroadcaster
{
    private const string BroadcastJobUpdateMethod = "BroadcastJobUpdate";
    private const string BroadcastJobRemovedMethod = "BroadcastJobRemoved";

    private HubConnection? _hubConnection;

    public bool IsConnected =>
        _hubConnection?.State == HubConnectionState.Connected;

    public async Task EnsureConnectedAsync(CancellationToken ct)
    {
        _hubConnection ??= new HubConnectionBuilder()
            .WithUrl(settings.Value.HubUrl)
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters
                    .Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            })
            .Build();

        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            logger.LogInformation("Connecting to SignalR hub at {HubUrl}.", settings.Value.HubUrl);
            await _hubConnection.StartAsync(ct);
            logger.LogInformation("Connected to SignalR hub.");
        }
    }

    public async Task BroadcastJobUpdateAsync(ResourcePlannerJobDto job, CancellationToken ct)
    {
        EnsureHubReady();
        await _hubConnection!.SendAsync(BroadcastJobUpdateMethod, job, cancellationToken: ct);
    }

    public async Task BroadcastJobRemovedAsync(int jobId, CancellationToken ct)
    {
        EnsureHubReady();
        await _hubConnection!.SendAsync(BroadcastJobRemovedMethod, jobId, cancellationToken: ct);
    }

    private void EnsureHubReady()
    {
        if (_hubConnection is null || _hubConnection.State != HubConnectionState.Connected)
            throw new InvalidOperationException(
                "SignalR hub is not connected. Call EnsureConnectedAsync before broadcasting.");
    }
}

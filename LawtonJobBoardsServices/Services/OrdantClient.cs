using LawtonJobBoardsServices.Models.Ordant;
using LawtonJobBoardsServices.Services.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;

namespace LawtonJobBoardsServices.Services;

public class OrdantClient(HttpClient http, OrdantTokenService tokenService) : IOrdantClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // Serializer fields requested for the order list endpoint.
    private static readonly List<KeyValuePair<string, string?>> OrderListSerializer =
    [
        new("serializer[]", "id"),
        new("serializer[]", "internalId"),
        new("serializer[]", "projectName"),
        new("serializer[]", "dueDate"),
        new("serializer[]", "isComplete"),
        new("serializer[]", "progress"),
        new("serializer[]", "status"),
        new("serializer[]", "priority"),
        new("serializer[]", "creationDate"),
        new("serializer[]", "sortOrder"),
        new("serializer[]", "summary"),
        new("serializer[1][customer][]", "id"),
        new("serializer[1][customer][]", "type"),
        new("serializer[1][customer][]", "fullName"),
        new("serializer[1][customer][]", "company"),
        new("serializer[1][customer][]", "name"),
    ];

    // ── Orders ────────────────────────────────────────────────────────────────

    public async Task<OrdantPagedResponse<OrdantOrderSummary>?> GetOrdersAsync(
        int page = 1,
        int limitPerPage = 100,
        bool? isComplete = null,
        string? statusFilter = null,
        CancellationToken ct = default)
    {
        var query = new List<KeyValuePair<string, string?>>(OrderListSerializer)
        {
            new("page", page.ToString()),
            new("limitPerPage", limitPerPage.ToString()),
        };

        if (isComplete.HasValue)
            query.Add(new("criteria[and][]", $"isComplete eq {isComplete.Value.ToString().ToLower()}"));

        if (!string.IsNullOrWhiteSpace(statusFilter))
            query.Add(new("criteria[and][]", $"status.value like %{statusFilter}%"));

        query.Add(new("criteria[order][]", "dueDate ASC"));

        var url = QueryHelpers.AddQueryString("order", query);
        var response = await ExecuteGetAsync(url, ct);
        return await response.Content.ReadFromJsonAsync<OrdantPagedResponse<OrdantOrderSummary>>(JsonOptions, ct);
    }

    public async Task<OrdantOrderDetail?> GetOrderAsync(int orderId, CancellationToken ct = default)
    {
        var response = await ExecuteGetAsync($"order/{orderId}", ct);
        return await response.Content.ReadFromJsonAsync<OrdantOrderDetail>(JsonOptions, ct);
    }

    // ── Resource Planner ──────────────────────────────────────────────────────

    public async Task<OrdantPagedResponse<OrdantSchedulerJob>?> GetSchedulerJobsAsync(
        int page = 1,
        int limitPerPage = 100,
        bool? isComplete = null,
        int? stationId = null,
        bool? hasCompletedDependencies = null,
        CancellationToken ct = default)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("page", page.ToString()),
            new("limitPerPage", limitPerPage.ToString()),
        };

        if (isComplete.HasValue)
            query.Add(new("criteria[and][]", $"isComplete eq {isComplete.Value.ToString().ToLower()}"));

        if (stationId.HasValue)
            query.Add(new("criteria[and][]", $"station eq {stationId.Value}"));

        if (hasCompletedDependencies.HasValue)
            query.Add(new("criteria[and][]", $"hasCompletedDependencies eq {hasCompletedDependencies.Value.ToString().ToLower()}"));

        query.Add(new("criteria[order][]", "stationSortOrder ASC"));

        var url = QueryHelpers.AddQueryString("scheduler/job", query);
        var response = await ExecuteGetAsync(url, ct);
        return await response.Content.ReadFromJsonAsync<OrdantPagedResponse<OrdantSchedulerJob>>(JsonOptions, ct);
    }

    public async Task<OrdantSchedulerJob?> GetSchedulerJobAsync(int jobId, CancellationToken ct = default)
    {
        var response = await ExecuteGetAsync($"scheduler/job/{jobId}", ct);
        return await response.Content.ReadFromJsonAsync<OrdantSchedulerJob>(JsonOptions, ct);
    }

    // ── Order Items ───────────────────────────────────────────────────────────

    // Fields the list serializer can return for order items.
    private static readonly List<KeyValuePair<string, string?>> OrderItemListSerializer =
    [
        new("serializer[]", "id"),
        new("serializer[]", "sku"),
        new("serializer[]", "sortId"),
        new("serializer[]", "sortOrder"),
        new("serializer[]", "qty"),
        new("serializer[]", "dateDue"),
        new("serializer[]", "dateShipBy"),
        new("serializer[]", "dateProofDue"),
        new("serializer[]", "creationDate"),
        new("serializer[]", "dateLastUpdated"),
        new("serializer[1][order][]", "id"),
        new("serializer[1][order][]", "internalId"),
        new("serializer[1][order][]", "type"),
        new("serializer[1][order][]", "projectName"),
    ];

    public async Task<OrdantPagedResponse<OrdantOrderItemSummary>?> GetOrderItemsAsync(
        int page = 1,
        int limitPerPage = 100,
        bool? isComplete = null,
        int? orderId = null,
        CancellationToken ct = default)
    {
        var query = new List<KeyValuePair<string, string?>>(OrderItemListSerializer)
        {
            new("page", page.ToString()),
            new("limitPerPage", limitPerPage.ToString()),
        };

        if (isComplete.HasValue)
            query.Add(new("criteria[and][]", $"orderiscomplete eq {isComplete.Value.ToString().ToLower()}"));

        if (orderId.HasValue)
            query.Add(new("criteria[and][]", $"order eq {orderId.Value}"));

        query.Add(new("criteria[order][]", "orderduedate ASC"));

        var url = QueryHelpers.AddQueryString("order-item", query);
        var response = await ExecuteGetAsync(url, ct);
        return await response.Content.ReadFromJsonAsync<OrdantPagedResponse<OrdantOrderItemSummary>>(JsonOptions, ct);
    }

    public async Task<OrdantOrderItemDetail?> GetOrderItemAsync(int itemId, CancellationToken ct = default)
    {
        var response = await ExecuteGetAsync($"order-item/{itemId}", ct);
        return await response.Content.ReadFromJsonAsync<OrdantOrderItemDetail>(JsonOptions, ct);
    }

    // ── Token-aware request helper ────────────────────────────────────────────

    private async Task<HttpResponseMessage> ExecuteGetAsync(string url, CancellationToken ct)
    {
        var token = await tokenService.GetTokenAsync(ct);
        var response = await http.GetAsync(WithToken(url, token), ct);

        // 419 = token expired; re-authenticate and retry once
        if ((int)response.StatusCode == 419)
        {
            tokenService.InvalidateToken();
            token = await tokenService.GetTokenAsync(ct);
            response = await http.GetAsync(WithToken(url, token), ct);
        }

        // Ordant returns a refreshed token on every response — keep it current
        if (response.Headers.TryGetValues("X-Authentication", out var values))
            tokenService.RefreshToken(values.First());

        response.EnsureSuccessStatusCode();
        return response;
    }

    private static string WithToken(string url, string token)
    {
        var separator = url.Contains('?') ? '&' : '?';
        return $"{url}{separator}token={token}";
    }
}

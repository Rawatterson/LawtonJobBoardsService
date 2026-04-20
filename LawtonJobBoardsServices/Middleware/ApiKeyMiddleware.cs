using LawtonJobBoardsServices.Configuration;
using Microsoft.Extensions.Options;

namespace LawtonJobBoardsServices.Middleware;

public class ApiKeyMiddleware(RequestDelegate next, IOptions<ApiSettings> settings, IWebHostEnvironment env)
{
    private const string HeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auth entirely in Development — no key needed for local testing.
        if (env.IsDevelopment())
        {
            await next(context);
            return;
        }

        // Let the OpenAPI spec and Scalar UI load without a key —
        // these routes only exist in Development and are not API endpoints.
        if (context.Request.Path.StartsWithSegments("/openapi") ||
            context.Request.Path.StartsWithSegments("/scalar"))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var provided) ||
            !string.Equals(provided, settings.Value.ApiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("""{"error":"Unauthorized. Provide a valid X-Api-Key header."}""");
            return;
        }

        await next(context);
    }
}

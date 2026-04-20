using LawtonJobBoardsServices.Configuration;
using LawtonJobBoardsServices.Middleware;
using LawtonJobBoardsServices.Services;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Declare the X-Api-Key security scheme so Scalar's auth panel can use it
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((doc, context, ct) =>
    {
        var components = doc.Components ??= new OpenApiComponents();
        components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        components.SecuritySchemes["ApiKey"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "X-Api-Key",
            Description = "API key passed in the X-Api-Key request header."
        };
        (doc.Security ??= []).Add(new OpenApiSecurityRequirement
        {
            { new OpenApiSecuritySchemeReference("ApiKey"), [] }
        });
        return Task.CompletedTask;
    });
});

// Ordant integration
var ordantSettings = builder.Configuration.GetSection("Ordant").Get<OrdantSettings>()
    ?? throw new InvalidOperationException("Ordant configuration section is missing.");

builder.Services.Configure<OrdantSettings>(builder.Configuration.GetSection("Ordant"));
builder.Services.AddSingleton<DueStatusCalculator>();
builder.Services.AddSingleton<OrdantTokenService>();

builder.Services.AddHttpClient<OrdantClient>(client =>
{
    client.BaseAddress = new Uri(ordantSettings.BaseUrl.TrimEnd('/') + "/");
});

// API key auth
var apiSettings = builder.Configuration.GetSection("Api").Get<ApiSettings>()
    ?? throw new InvalidOperationException("Api configuration section is missing.");

builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("Api"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Lawton Job Boards API";
        options.Theme = ScalarTheme.DeepSpace;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
        // Pre-populate the API key in Scalar's auth panel for frictionless dev testing
        options.AddApiKeyAuthentication("ApiKey", scheme => scheme.Value = apiSettings.ApiKey);
    });
}

// Must come before routing so unauthenticated requests are rejected
// before any controller logic runs.
app.UseMiddleware<ApiKeyMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

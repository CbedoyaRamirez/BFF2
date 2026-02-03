using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SpeechService API", Version = "v1" });
});

// Health checks mejorados
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("SpeechService API is running"), tags: new[] { "ready", "live" })
    .AddCheck("memory", () =>
    {
        var allocatedBytes = GC.GetTotalMemory(forceFullCollection: false);
        var allocatedMB = allocatedBytes / 1024 / 1024;

        var data = new Dictionary<string, object>
        {
            { "allocatedMB", allocatedMB },
            { "gen0Collections", GC.CollectionCount(0) },
            { "gen1Collections", GC.CollectionCount(1) },
            { "gen2Collections", GC.CollectionCount(2) }
        };

        var status = allocatedMB > 500 ? HealthStatus.Degraded : HealthStatus.Healthy;
        var description = allocatedMB > 500 ? "High memory usage" : "Memory usage is normal";

        return new HealthCheckResult(status, description, data: data);
    }, tags: new[] { "ready" })
    .AddCheck("uptime", () =>
    {
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();

        var data = new Dictionary<string, object>
        {
            { "uptime", uptime.ToString() },
            { "startTime", Process.GetCurrentProcess().StartTime.ToUniversalTime() }
        };

        return HealthCheckResult.Healthy("Service is running", data);
    }, tags: new[] { "live" });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Health check endpoints con respuestas detalladas
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            service = "SpeechService",
            version = "1.0.0",
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.Run();

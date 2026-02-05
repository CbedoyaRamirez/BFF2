using Chubb.Bot.AI.Assistant.Api.Helpers;
using Chubb.Bot.AI.Assistant.Api.Middleware;
using Chubb.Bot.AI.Assistant.Application.Validators;
using Chubb.Bot.AI.Assistant.Infrastructure.HealthChecks;
using Chubb.Bot.AI.Assistant.Infrastructure.HttpClients;
using Chubb.Bot.AI.Assistant.Infrastructure.HttpClients.Handlers;
using Chubb.Bot.AI.Assistant.Infrastructure.HttpClients.Interfaces;
using Chubb.Bot.AI.Assistant.Infrastructure.Policies;
using Chubb.Bot.AI.Assistant.Infrastructure.Redis;
using FluentValidation;
using FluentValidation.AspNetCore;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Context;
using Serilog.Expressions;
using System.Text;
using System.Threading.RateLimiting;

// Inicializar directorios de logs
LoggingHelper.InitializeLogDirectories();

// Configurar Serilog desde appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting Chubb Bot AI Assistant API");
    LoggingHelper.LogDevelopment("Application starting in {Environment} mode", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");

var builder = WebApplication.CreateBuilder(args);

// Agregar Serilog
builder.Host.UseSerilog();

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger/OpenAPI
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BFF API",
        Version = "v1",
        Description = "Backend For Frontend API for Chubb"
    });

    // JWT Authentication for Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "*" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

// Redis - COMENTADO TEMPORALMENTE
/*
var redisConnectionString = builder.Configuration["RedisSettings:ConnectionString"] ?? "localhost:6379";
RedisConnectionFactory.Initialize(redisConnectionString);

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = builder.Configuration["RedisSettings:InstanceName"] ?? "BFF:";
});
*/

// Session Management con Redis - COMENTADO TEMPORALMENTE
/*
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("RedisSettings:DefaultTTLMinutes", 30));
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
*/

// Rate Limiting Nativo de .NET 8
builder.Services.AddRateLimiter(options =>
{
    // Configurar rechazo con 429 Too Many Requests
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Política global - Fixed Window: 60 requests por minuto
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Política "api" - Para endpoints de API: 100 requests por minuto
    options.AddFixedWindowLimiter("api", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });

    // Política "health" - Para health checks: 300 requests por minuto
    options.AddFixedWindowLimiter("health", options =>
    {
        options.PermitLimit = 300;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });

    // Política "strict" - Para operaciones críticas: 10 requests por minuto
    options.AddFixedWindowLimiter("strict", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });

    // Respuesta personalizada cuando se excede el límite
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
            ? retryAfterValue.TotalSeconds
            : 60;

        context.HttpContext.Response.Headers["Retry-After"] = retryAfter.ToString();

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too Many Requests",
            message = $"Rate limit exceeded. Please try again in {retryAfter} seconds.",
            retryAfter = retryAfter
        }, cancellationToken: token);
    };
});

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<ChatRequestValidator>();

// HttpContextAccessor (needed for CorrelationIdDelegatingHandler)
builder.Services.AddHttpContextAccessor();

// Session Service - COMENTADO TEMPORALMENTE (requiere Redis)
// builder.Services.AddScoped<Chubb.Bot.AI.Assistant.Infrastructure.Services.Interfaces.ISessionService, Chubb.Bot.AI.Assistant.Infrastructure.Services.SessionService>();

// Delegating Handlers
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
builder.Services.AddTransient<LoggingDelegatingHandler>();

// HTTP Clients with Polly
var httpClientConfig = builder.Configuration.GetSection("HttpClients");

// ChatBot Client
builder.Services.AddHttpClient<IChatBotClient, ChatBotClient>(client =>
{
    var baseUrl = httpClientConfig.GetValue<string>("ChatBot:BaseUrl") ?? "http://localhost:5266";
    client.BaseAddress = new Uri(baseUrl);
})
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
.AddHttpMessageHandler<LoggingDelegatingHandler>()
.AddPolicyHandler(PollyPolicies.GetCombinedPolicy(
    retryCount: httpClientConfig.GetValue<int>("ChatBot:RetryCount", 3),
    circuitBreakerThreshold: httpClientConfig.GetValue<int>("ChatBot:CircuitBreakerThreshold", 5),
    circuitBreakerDuration: httpClientConfig.GetValue<int>("ChatBot:CircuitBreakerDurationSeconds", 30),
    timeoutSeconds: httpClientConfig.GetValue<int>("ChatBot:TimeoutSeconds", 10)));

// FAQBot Client
builder.Services.AddHttpClient<IFAQBotClient, FAQBotClient>(client =>
{
    var baseUrl = httpClientConfig.GetValue<string>("FAQBot:BaseUrl") ?? "http://localhost:5267";
    client.BaseAddress = new Uri(baseUrl);
})
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
.AddHttpMessageHandler<LoggingDelegatingHandler>()
.AddPolicyHandler(PollyPolicies.GetCombinedPolicy(
    retryCount: httpClientConfig.GetValue<int>("FAQBot:RetryCount", 3),
    circuitBreakerThreshold: httpClientConfig.GetValue<int>("FAQBot:CircuitBreakerThreshold", 5),
    circuitBreakerDuration: httpClientConfig.GetValue<int>("FAQBot:CircuitBreakerDurationSeconds", 30),
    timeoutSeconds: httpClientConfig.GetValue<int>("FAQBot:TimeoutSeconds", 10)));

// Speech Service Client
builder.Services.AddHttpClient<ISpeechClient, SpeechClient>(client =>
{
    var baseUrl = httpClientConfig.GetValue<string>("SpeechService:BaseUrl") ?? "http://localhost:7001";
    client.BaseAddress = new Uri(baseUrl);
})
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
.AddHttpMessageHandler<LoggingDelegatingHandler>()
.AddPolicyHandler(PollyPolicies.GetCombinedPolicy(
    retryCount: httpClientConfig.GetValue<int>("SpeechService:RetryCount", 2),
    circuitBreakerThreshold: httpClientConfig.GetValue<int>("SpeechService:CircuitBreakerThreshold", 5),
    circuitBreakerDuration: httpClientConfig.GetValue<int>("SpeechService:CircuitBreakerDurationSeconds", 30),
    timeoutSeconds: httpClientConfig.GetValue<int>("SpeechService:TimeoutSeconds", 30)));

// Health Checks mejorados
var chatBotBaseUrl = httpClientConfig.GetValue<string>("ChatBot:BaseUrl") ?? "http://localhost:5266";
var faqBotBaseUrl = httpClientConfig.GetValue<string>("FAQBot:BaseUrl") ?? "http://localhost:5267";
var speechServiceBaseUrl = httpClientConfig.GetValue<string>("SpeechService:BaseUrl") ?? "http://localhost:7001";

builder.Services.AddHealthChecks()
    // Self check
    .AddCheck("self", () =>
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
        var memoryMB = process.WorkingSet64 / 1024 / 1024;

        var data = new Dictionary<string, object>
        {
            { "uptime", uptime.ToString() },
            { "memoryUsageMB", memoryMB }
        };

        return HealthCheckResult.Healthy("BFF API is running", data);
    }, tags: new[] { "ready", "live" })
    // Redis Health Check - COMENTADO TEMPORALMENTE
    // .AddCheck<RedisHealthCheck>("redis", tags: new[] { "db", "redis", "ready" })
    // ChatBot Health Check con custom implementation
    .AddTypeActivatedCheck<HttpEndpointHealthCheck>(
        "chatbot",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external", "services" },
        args: new object[] { $"{chatBotBaseUrl}/health", "ChatBot" })
    // FAQBot Health Check
    .AddTypeActivatedCheck<HttpEndpointHealthCheck>(
        "faqbot",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external", "services" },
        args: new object[] { $"{faqBotBaseUrl}/health", "FAQBot" })
    // SpeechService Health Check
    .AddTypeActivatedCheck<HttpEndpointHealthCheck>(
        "speechservice",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external", "services" },
        args: new object[] { $"{speechServiceBaseUrl}/health", "SpeechService" });

var app = builder.Build();

// Middleware Pipeline

// Serilog Request Logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());

        if (httpContext.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            diagnosticContext.Set("CorrelationId", correlationId);
        }
    };
});

// Custom Exception Handling
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

// Rate Limiting Nativo
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BFF API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors();

// Session Management - COMENTADO TEMPORALMENTE (requiere Redis)
// app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Health Check Endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).RequireRateLimiting("health");

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).RequireRateLimiting("health");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).RequireRateLimiting("health");

app.MapControllers();

app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

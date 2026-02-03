using AspNetCoreRateLimit;
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
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Context;
using System.Text;

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

// Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.Configure<ClientRateLimitOptions>(builder.Configuration.GetSection("ClientRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

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

// Health Checks
builder.Services.AddHealthChecks()
    // Redis Health Check - COMENTADO TEMPORALMENTE
    // .AddCheck<RedisHealthCheck>("redis", tags: new[] { "db", "redis", "ready" })
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"), tags: new[] { "ready" })
    .AddUrlGroup(
        new Uri($"{httpClientConfig.GetValue<string>("ChatBot:BaseUrl") ?? "http://localhost:5266"}/health"),
        name: "chatbot",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external", "services" },
        timeout: TimeSpan.FromSeconds(5))
    .AddUrlGroup(
        new Uri($"{httpClientConfig.GetValue<string>("FAQBot:BaseUrl") ?? "http://localhost:5267"}/health"),
        name: "faqbot",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external", "services" },
        timeout: TimeSpan.FromSeconds(5))
    .AddUrlGroup(
        new Uri($"{httpClientConfig.GetValue<string>("SpeechService:BaseUrl") ?? "http://localhost:7001"}/health"),
        name: "speechservice",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external", "services" },
        timeout: TimeSpan.FromSeconds(5));

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

// Rate Limiting
app.UseIpRateLimiting();

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
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

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

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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration
// Note: Serilog se puede configurar aquí o en appsettings.json
// Para este ejemplo, se asume configuración desde appsettings.json

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

// Redis
var redisConnectionString = builder.Configuration["RedisSettings:ConnectionString"] ?? "localhost:6379";
RedisConnectionFactory.Initialize(redisConnectionString);

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = builder.Configuration["RedisSettings:InstanceName"] ?? "BFF:";
});

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<ChatRequestValidator>();

// HttpContextAccessor (needed for CorrelationIdDelegatingHandler)
builder.Services.AddHttpContextAccessor();

// Delegating Handlers
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
builder.Services.AddTransient<LoggingDelegatingHandler>();

// HTTP Clients with Polly
var httpClientConfig = builder.Configuration.GetSection("HttpClients");

// QuoteBot Client
builder.Services.AddHttpClient<IQuoteBotClient, QuoteBotClient>(client =>
{
    var baseUrl = httpClientConfig.GetValue<string>("QuoteBot:BaseUrl") ?? "http://localhost:5266";
    client.BaseAddress = new Uri(baseUrl);
})
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
.AddHttpMessageHandler<LoggingDelegatingHandler>()
.AddPolicyHandler(PollyPolicies.GetCombinedPolicy(
    retryCount: httpClientConfig.GetValue<int>("QuoteBot:RetryCount", 3),
    circuitBreakerThreshold: httpClientConfig.GetValue<int>("QuoteBot:CircuitBreakerThreshold", 5),
    circuitBreakerDuration: httpClientConfig.GetValue<int>("QuoteBot:CircuitBreakerDurationSeconds", 30),
    timeoutSeconds: httpClientConfig.GetValue<int>("QuoteBot:TimeoutSeconds", 10)));

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
    .AddCheck<RedisHealthCheck>("redis", tags: new[] { "db", "redis" })
    .AddUrlGroup(new Uri($"{httpClientConfig.GetValue<string>("QuoteBot:BaseUrl") ?? "http://localhost:5266"}/health"), "quotebot", tags: new[] { "external" })
    .AddUrlGroup(new Uri($"{httpClientConfig.GetValue<string>("FAQBot:BaseUrl") ?? "http://localhost:5267"}/health"), "faqbot", tags: new[] { "external" })
    .AddUrlGroup(new Uri($"{httpClientConfig.GetValue<string>("SpeechService:BaseUrl") ?? "http://localhost:7001"}/health"), "speechservice", tags: new[] { "external" });

var app = builder.Build();

// Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Bot.AI.Assistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    private readonly ILogger<SystemController> _logger;

    public SystemController(ILogger<SystemController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Obtiene información del sistema y configuración
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(typeof(SystemInfo), StatusCodes.Status200OK)]
    public ActionResult<SystemInfo> GetSystemInfo()
    {
        var info = new SystemInfo
        {
            ApplicationName = "Chubb Bot AI Assistant API",
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            MachineName = Environment.MachineName,
            OsVersion = Environment.OSVersion.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            UpTime = GetUptime(),
            Timestamp = DateTime.UtcNow
        };

        return Ok(info);
    }

    /// <summary>
    /// Obtiene la configuración actual de Rate Limiting (Sistema Nativo .NET 8)
    /// </summary>
    [HttpGet("rate-limit-config")]
    [ProducesResponseType(typeof(RateLimitConfig), StatusCodes.Status200OK)]
    public ActionResult<RateLimitConfig> GetRateLimitConfig()
    {
        var config = new RateLimitConfig
        {
            Type = "Native .NET 8 Rate Limiter",
            Algorithm = "Fixed Window",
            HttpStatusCode = 429,
            Policies = new List<RateLimitPolicyInfo>
            {
                new RateLimitPolicyInfo
                {
                    Name = "global",
                    Description = "Política global aplicada a todos los endpoints",
                    PermitLimit = 60,
                    WindowMinutes = 1,
                    QueueLimit = 0
                },
                new RateLimitPolicyInfo
                {
                    Name = "api",
                    Description = "Política para endpoints de API (/api/*)",
                    PermitLimit = 100,
                    WindowMinutes = 1,
                    QueueLimit = 0
                },
                new RateLimitPolicyInfo
                {
                    Name = "health",
                    Description = "Política para health checks (/health*)",
                    PermitLimit = 300,
                    WindowMinutes = 1,
                    QueueLimit = 0
                },
                new RateLimitPolicyInfo
                {
                    Name = "strict",
                    Description = "Política estricta para operaciones críticas",
                    PermitLimit = 10,
                    WindowMinutes = 1,
                    QueueLimit = 0
                }
            }
        };

        return Ok(config);
    }

    /// <summary>
    /// Obtiene información de los endpoints disponibles
    /// </summary>
    [HttpGet("endpoints")]
    [ProducesResponseType(typeof(EndpointsInfo), StatusCodes.Status200OK)]
    public ActionResult<EndpointsInfo> GetEndpoints()
    {
        var endpoints = new EndpointsInfo
        {
            Health = new List<EndpointDetail>
            {
                new EndpointDetail { Path = "/health", Description = "Health check completo de todos los servicios", Method = "GET" },
                new EndpointDetail { Path = "/health/ready", Description = "Readiness check - API lista para tráfico", Method = "GET" },
                new EndpointDetail { Path = "/health/live", Description = "Liveness check - API está viva", Method = "GET" }
            },
            Chat = new List<EndpointDetail>
            {
                new EndpointDetail { Path = "/api/chat", Description = "Enviar mensaje al ChatBot", Method = "POST" }
            },
            FAQ = new List<EndpointDetail>
            {
                new EndpointDetail { Path = "/api/faq", Description = "Consultar FAQBot", Method = "POST" }
            },
            Speech = new List<EndpointDetail>
            {
                new EndpointDetail { Path = "/api/speech/tts", Description = "Text-to-Speech", Method = "POST" },
                new EndpointDetail { Path = "/api/speech/stt", Description = "Speech-to-Text", Method = "POST" }
            },
            System = new List<EndpointDetail>
            {
                new EndpointDetail { Path = "/api/system/info", Description = "Información del sistema", Method = "GET" },
                new EndpointDetail { Path = "/api/system/rate-limit-config", Description = "Configuración de Rate Limiting", Method = "GET" },
                new EndpointDetail { Path = "/api/system/endpoints", Description = "Lista de endpoints disponibles", Method = "GET" }
            }
        };

        return Ok(endpoints);
    }

    private TimeSpan GetUptime()
    {
        return DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
    }
}

// DTOs
public class SystemInfo
{
    public string ApplicationName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public TimeSpan UpTime { get; set; }
    public DateTime Timestamp { get; set; }
}

public class RateLimitConfig
{
    public string Type { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public int HttpStatusCode { get; set; }
    public List<RateLimitPolicyInfo> Policies { get; set; } = new();
}

public class RateLimitPolicyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PermitLimit { get; set; }
    public int WindowMinutes { get; set; }
    public int QueueLimit { get; set; }
}

public class EndpointsInfo
{
    public List<EndpointDetail> Health { get; set; } = new();
    public List<EndpointDetail> Chat { get; set; } = new();
    public List<EndpointDetail> FAQ { get; set; } = new();
    public List<EndpointDetail> Speech { get; set; } = new();
    public List<EndpointDetail> System { get; set; } = new();
}

public class EndpointDetail
{
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
}

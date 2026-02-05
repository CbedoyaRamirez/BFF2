using Chubb.Bot.AI.Assistant.Api.Helpers;
using Chubb.Bot.AI.Assistant.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Bot.AI.Assistant.Api.Controllers;

/// <summary>
/// Controller de prueba para validar el sistema de logging
/// SOLO PARA DESARROLLO - Eliminar en producción
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;

    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Prueba logs de error - Se debe escribir en logs/error/
    /// </summary>
    [HttpGet("error")]
    public IActionResult TestError()
    {
        throw new Exception("Este es un error de prueba para validar logs/error/");
    }

    /// <summary>
    /// Prueba logs de error con LoggingHelper - Se debe escribir en logs/error/
    /// </summary>
    [HttpGet("error-helper")]
    public IActionResult TestErrorHelper()
    {
        try
        {
            throw new InvalidOperationException("Error de prueba usando LoggingHelper");
        }
        catch (Exception ex)
        {
            LoggingHelper.LogError("Error capturado manualmente con LoggingHelper", ex);
            return StatusCode(500, new { message = "Error loggeado en logs/error/" });
        }
    }

    /// <summary>
    /// Prueba logs de performance - Se debe escribir en logs/performance/
    /// </summary>
    [HttpGet("performance")]
    public async Task<IActionResult> TestPerformance()
    {
        using (var perfLogger = LoggingHelper.LogPerformance("TestController.TestPerformance"))
        {
            perfLogger.AddContext("TestType", "Performance");
            perfLogger.AddContext("Endpoint", "/api/test/performance");

            // Simular operación que tarda tiempo
            await Task.Delay(150);

            return Ok(new
            {
                message = "Performance test completado",
                note = "Verifica logs/performance/ para ver el tiempo de ejecución"
            });
        }
    }

    /// <summary>
    /// Prueba logs de desarrollo - Se debe escribir en logs/dev/
    /// </summary>
    [HttpGet("development")]
    public IActionResult TestDevelopment()
    {
        LoggingHelper.LogDevelopment("Log de desarrollo de prueba desde TestController");

        var testObject = new
        {
            UserId = 12345,
            Action = "TestDevelopment",
            Timestamp = DateTime.UtcNow,
            Data = new[] { "item1", "item2", "item3" }
        };

        LoggingHelper.LogDevelopmentObject("Objeto de prueba", testObject);

        LoggingHelper.LogDevelopmentWarning("Este es un warning de desarrollo");

        return Ok(new
        {
            message = "Development logs generados",
            note = "Verifica logs/dev/ para ver los logs de desarrollo"
        });
    }

    /// <summary>
    /// Prueba BusinessException - Se debe loggear como warning
    /// </summary>
    [HttpGet("business-error")]
    public IActionResult TestBusinessError()
    {
        throw new BusinessException("Usuario no encontrado", "USER_NOT_FOUND");
    }

    /// <summary>
    /// Prueba de todos los tipos de log en una operación
    /// </summary>
    [HttpGet("all-logs")]
    public async Task<IActionResult> TestAllLogs()
    {
        using (var perfLogger = LoggingHelper.LogPerformance("TestController.TestAllLogs"))
        {
            try
            {
                LoggingHelper.LogDevelopment("Iniciando prueba de todos los tipos de log");

                perfLogger.AddContext("Step", "1-Development");
                await Task.Delay(50);

                LoggingHelper.LogDevelopmentObject("Request simulado", new
                {
                    Method = "GET",
                    Path = "/api/test/all-logs",
                    Timestamp = DateTime.UtcNow
                });

                perfLogger.AddContext("Step", "2-Processing");
                await Task.Delay(100);

                _logger.LogInformation("Log general: Procesando operación de prueba");

                perfLogger.AddContext("Step", "3-Completed");
                perfLogger.AddContext("ItemsProcessed", 42);

                return Ok(new
                {
                    message = "Prueba completa ejecutada",
                    generatedLogs = new
                    {
                        general = "logs/app-*.log",
                        performance = "logs/performance/performance-*.log",
                        development = "logs/dev/dev-*.log"
                    },
                    note = "Verifica cada archivo de log para ver los resultados"
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError("Error en prueba de todos los logs", ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Prueba timeout/cancelación
    /// </summary>
    [HttpGet("timeout")]
    public async Task<IActionResult> TestTimeout(CancellationToken cancellationToken)
    {
        // Simular operación larga
        await Task.Delay(10000, cancellationToken);
        return Ok("Completado");
    }

    /// <summary>
    /// Información sobre las pruebas disponibles
    /// </summary>
    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        return Ok(new
        {
            title = "Test Controller - Sistema de Logging",
            description = "Endpoints para probar el sistema de logging",
            endpoints = new[]
            {
                new
                {
                    path = "/api/test/error",
                    description = "Genera un error no manejado (escribe en logs/error/)",
                    expectedLog = "logs/error/error-*.log"
                },
                new
                {
                    path = "/api/test/error-helper",
                    description = "Error capturado con LoggingHelper (escribe en logs/error/)",
                    expectedLog = "logs/error/error-*.log"
                },
                new
                {
                    path = "/api/test/performance",
                    description = "Mide performance de operación (escribe en logs/performance/)",
                    expectedLog = "logs/performance/performance-*.log"
                },
                new
                {
                    path = "/api/test/development",
                    description = "Genera logs de desarrollo (escribe en logs/dev/)",
                    expectedLog = "logs/dev/dev-*.log"
                },
                new
                {
                    path = "/api/test/business-error",
                    description = "Genera un BusinessException (warning, NO en logs/error/)",
                    expectedLog = "logs/app-*.log"
                },
                new
                {
                    path = "/api/test/all-logs",
                    description = "Prueba todos los tipos de log en una sola operación",
                    expectedLog = "logs/app-*.log, logs/performance/*, logs/dev/*"
                }
            },
            verification = new
            {
                title = "Cómo verificar los logs",
                commands = new[]
                {
                    "tail -f logs/error/error-*.log",
                    "tail -f logs/performance/performance-*.log",
                    "tail -f logs/dev/dev-*.log",
                    "tail -f logs/app-*.log"
                }
            },
            note = "IMPORTANTE: Este controller es solo para pruebas. Eliminar antes de producción."
        });
    }
}

using Serilog;
using Serilog.Context;
using System.Diagnostics;

namespace Chubb.Bot.AI.Assistant.Api.Helpers;

/// <summary>
/// Helper class para facilitar el logging en diferentes categorías
/// </summary>
public static class LoggingHelper
{
    /// <summary>
    /// Inicializa las carpetas de logs al iniciar la aplicación
    /// </summary>
    public static void InitializeLogDirectories()
    {
        var logDirectories = new[]
        {
            "logs",
            "logs/error",
            "logs/performance",
            "logs/dev"
        };

        foreach (var directory in logDirectories)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Log.Information("Created log directory: {Directory}", directory);
            }
        }
    }

    /// <summary>
    /// Log de error que se escribirá en la carpeta logs/error
    /// </summary>
    public static void LogError(string message, Exception? exception = null, params object[] propertyValues)
    {
        if (exception != null)
        {
            Log.Error(exception, message, propertyValues);
        }
        else
        {
            Log.Error(message, propertyValues);
        }
    }

    /// <summary>
    /// Log de error fatal que se escribirá en la carpeta logs/error
    /// </summary>
    public static void LogFatal(string message, Exception? exception = null, params object[] propertyValues)
    {
        if (exception != null)
        {
            Log.Fatal(exception, message, propertyValues);
        }
        else
        {
            Log.Fatal(message, propertyValues);
        }
    }

    /// <summary>
    /// Log de performance que se escribirá en la carpeta logs/performance
    /// Mide el tiempo de ejecución de una operación
    /// </summary>
    public static PerformanceLogger LogPerformance(string operationName)
    {
        return new PerformanceLogger(operationName);
    }

    /// <summary>
    /// Log de desarrollo que se escribirá en la carpeta logs/dev
    /// Útil para debugging y desarrollo
    /// </summary>
    public static void LogDevelopment(string message, params object[] propertyValues)
    {
        using (LogContext.PushProperty("Category", "Development"))
        using (LogContext.PushProperty("DevLog", true))
        {
            Log.Information(message, propertyValues);
        }
    }

    /// <summary>
    /// Log de desarrollo con nivel Warning
    /// </summary>
    public static void LogDevelopmentWarning(string message, params object[] propertyValues)
    {
        using (LogContext.PushProperty("Category", "Development"))
        using (LogContext.PushProperty("DevLog", true))
        {
            Log.Warning(message, propertyValues);
        }
    }

    /// <summary>
    /// Log de desarrollo con detalles de un objeto (serializado como JSON)
    /// </summary>
    public static void LogDevelopmentObject(string message, object obj)
    {
        using (LogContext.PushProperty("Category", "Development"))
        using (LogContext.PushProperty("DevLog", true))
        using (LogContext.PushProperty("ObjectData", obj, destructureObjects: true))
        {
            Log.Information(message);
        }
    }
}

/// <summary>
/// Clase para medir y loggear el rendimiento de operaciones
/// Se usa con using para medir automáticamente el tiempo
/// </summary>
public class PerformanceLogger : IDisposable
{
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;
    private readonly IDisposable? _performanceContext;

    public PerformanceLogger(string operationName)
    {
        _operationName = operationName;
        _stopwatch = Stopwatch.StartNew();
        _performanceContext = LogContext.PushProperty("Category", "Performance");
    }

    public void Dispose()
    {
        _stopwatch.Stop();

        using (LogContext.PushProperty("OperationName", _operationName))
        using (LogContext.PushProperty("ElapsedMilliseconds", _stopwatch.ElapsedMilliseconds))
        using (LogContext.PushProperty("ElapsedTicks", _stopwatch.ElapsedTicks))
        {
            Log.Information(
                "Performance: {OperationName} completed in {ElapsedMs}ms",
                _operationName,
                _stopwatch.ElapsedMilliseconds
            );
        }

        _performanceContext?.Dispose();
    }

    /// <summary>
    /// Agrega información adicional al log de performance
    /// </summary>
    public void AddContext(string key, object value)
    {
        LogContext.PushProperty(key, value);
    }
}

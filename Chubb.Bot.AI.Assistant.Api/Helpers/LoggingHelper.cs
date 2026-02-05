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
    /// IMPORTANTE: Debe llamarse DESPUÉS de configurar Serilog
    /// </summary>
    public static void InitializeLogDirectories()
    {
        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            Console.WriteLine($"[INIT] Current directory: {currentDir}");

            var baseDir = "logs";
            var baseDirFullPath = Path.Combine(currentDir, baseDir);

            // 1. Verificar/Crear directorio base 'logs'
            if (!Directory.Exists(baseDirFullPath))
            {
                Console.WriteLine($"[INIT] Creating base log directory: {baseDirFullPath}");
                Directory.CreateDirectory(baseDirFullPath);
                Console.WriteLine($"[INIT] ✓ Created: {baseDirFullPath}");
            }
            else
            {
                Console.WriteLine($"[INIT] Base log directory exists: {baseDirFullPath}");
            }

            // 2. Si logs existe, validar y crear subcarpetas
            if (Directory.Exists(baseDirFullPath))
            {
                var subDirectories = new[] { "error", "performance", "dev" };

                foreach (var subDir in subDirectories)
                {
                    var fullPath = Path.Combine(baseDirFullPath, subDir);

                    try
                    {
                        if (!Directory.Exists(fullPath))
                        {
                            Console.WriteLine($"[INIT] Creating subdirectory: {fullPath}");
                            Directory.CreateDirectory(fullPath);
                            Console.WriteLine($"[INIT] ✓ Created: logs\\{subDir}");

                            if (Log.Logger != null)
                            {
                                Log.Information("Created log subdirectory: {SubDirectory}", subDir);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[INIT] ✓ Exists: logs\\{subDir}");
                        }
                    }
                    catch (Exception subEx)
                    {
                        Console.WriteLine($"[INIT] ✗ Error creating logs\\{subDir}: {subEx.Message}");
                        if (Log.Logger != null)
                        {
                            Log.Error(subEx, "Failed to create log subdirectory: {SubDirectory}", subDir);
                        }
                    }
                }

                // 3. Verificar resultado final
                Console.WriteLine($"[INIT] Verification - Directory structure:");
                Console.WriteLine($"[INIT]   logs\\ - {(Directory.Exists(baseDirFullPath) ? "✓" : "✗")}");
                Console.WriteLine($"[INIT]   logs\\error\\ - {(Directory.Exists(Path.Combine(baseDirFullPath, "error")) ? "✓" : "✗")}");
                Console.WriteLine($"[INIT]   logs\\performance\\ - {(Directory.Exists(Path.Combine(baseDirFullPath, "performance")) ? "✓" : "✗")}");
                Console.WriteLine($"[INIT]   logs\\dev\\ - {(Directory.Exists(Path.Combine(baseDirFullPath, "dev")) ? "✓" : "✗")}");
            }
            else
            {
                Console.WriteLine($"[INIT] ✗ CRITICAL: Base log directory could not be created!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[INIT] ✗ CRITICAL ERROR in InitializeLogDirectories: {ex.Message}");
            Console.WriteLine($"[INIT] Stack trace: {ex.StackTrace}");

            if (Log.Logger != null)
            {
                Log.Fatal(ex, "Critical error initializing log directories");
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

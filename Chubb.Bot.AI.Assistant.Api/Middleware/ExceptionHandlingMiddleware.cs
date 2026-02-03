using System.Net;
using System.Text.Json;
using Chubb.Bot.AI.Assistant.Application.DTOs.Common;
using Chubb.Bot.AI.Assistant.Core.Exceptions;
using FluentValidation;
using Serilog;

namespace Chubb.Bot.AI.Assistant.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        // Enriquecer el contexto de log
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        using (Serilog.Context.LogContext.PushProperty("RequestPath", requestPath))
        using (Serilog.Context.LogContext.PushProperty("RequestMethod", requestMethod))
        {
            var errorResponse = new ErrorResponse
            {
                TraceId = correlationId,
                Timestamp = DateTime.UtcNow
            };

            int statusCode;

            switch (exception)
            {
                case ValidationException validationException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.ErrorCode = "VALIDATION_ERROR";
                    errorResponse.Message = "Validation failed";
                    errorResponse.Details = validationException.Errors
                        .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
                        .ToList();
                    _logger.LogWarning(validationException, "Validation error on {Path}", requestPath);
                    break;

                case BusinessException businessException:
                    statusCode = businessException.StatusCode;
                    errorResponse.ErrorCode = businessException.ErrorCode;
                    errorResponse.Message = businessException.Message;
                    _logger.LogWarning(businessException, "Business exception: {ErrorCode} - {Message}",
                        businessException.ErrorCode, businessException.Message);
                    break;

                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.ErrorCode = "UNAUTHORIZED";
                    errorResponse.Message = "Unauthorized access";
                    _logger.LogWarning(exception, "Unauthorized access attempt on {Path}", requestPath);
                    break;

                case TaskCanceledException:
                case OperationCanceledException:
                    statusCode = (int)HttpStatusCode.RequestTimeout;
                    errorResponse.ErrorCode = "REQUEST_TIMEOUT";
                    errorResponse.Message = "The request was cancelled or timed out";
                    _logger.LogWarning("Request timeout on {Path}", requestPath);
                    break;

                default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.ErrorCode = "INTERNAL_ERROR";
                    errorResponse.Message = _environment.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred. Please contact support with the trace ID.";

                    if (_environment.IsDevelopment())
                    {
                        errorResponse.Details = new List<string>
                        {
                            $"Exception Type: {exception.GetType().Name}",
                            $"Stack Trace: {exception.StackTrace}"
                        };
                    }

                    _logger.LogError(exception, "Unhandled exception on {Path}: {ExceptionType}",
                        requestPath, exception.GetType().Name);
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            };

            var json = JsonSerializer.Serialize(errorResponse, options);
            await context.Response.WriteAsync(json);
        }
    }
}

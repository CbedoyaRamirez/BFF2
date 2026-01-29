using System.Net;
using System.Text.Json;
using Chubb.Bot.AI.Assistant.Application.DTOs.Common;
using Chubb.Bot.AI.Assistant.Core.Exceptions;

namespace Chubb.Bot.AI.Assistant.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
        var correlationId = context.Items["CorrelationId"]?.ToString();

        _logger.LogError(exception, "An error occurred. CorrelationId: {CorrelationId}", correlationId);

        var errorResponse = new ErrorResponse
        {
            TraceId = correlationId,
            Timestamp = DateTime.UtcNow
        };

        int statusCode;

        if (exception is BusinessException businessException)
        {
            statusCode = businessException.StatusCode;
            errorResponse.ErrorCode = businessException.ErrorCode;
            errorResponse.Message = businessException.Message;
        }
        else
        {
            statusCode = (int)HttpStatusCode.InternalServerError;
            errorResponse.ErrorCode = "BFF_1000";
            errorResponse.Message = "An unexpected error occurred";
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var json = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(json);
    }
}

namespace Chubb.Bot.AI.Assistant.Core.Exceptions;

public class ExternalServiceException : BusinessException
{
    public string ServiceName { get; set; }

    public ExternalServiceException(string serviceName, string message, string errorCode = "BFF_4000")
        : base($"External service '{serviceName}' error: {message}", errorCode, 503)
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException, string errorCode = "BFF_4000")
        : base($"External service '{serviceName}' error: {message}", innerException, errorCode, 503)
    {
        ServiceName = serviceName;
    }
}

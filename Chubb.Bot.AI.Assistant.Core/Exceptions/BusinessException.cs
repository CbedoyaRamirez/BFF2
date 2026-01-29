namespace Chubb.Bot.AI.Assistant.Core.Exceptions;

public class BusinessException : Exception
{
    public string ErrorCode { get; set; }
    public int StatusCode { get; set; }

    public BusinessException(string message, string errorCode = "BFF_1000", int statusCode = 400)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public BusinessException(string message, Exception innerException, string errorCode = "BFF_1000", int statusCode = 400)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

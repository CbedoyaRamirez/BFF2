namespace Chubb.Bot.AI.Assistant.Core.Constants;

public static class ErrorCodes
{
    // General Errors (1000-1999)
    public const string GENERAL_ERROR = "BFF_1000";
    public const string VALIDATION_ERROR = "BFF_1001";
    public const string UNAUTHORIZED = "BFF_1002";
    public const string FORBIDDEN = "BFF_1003";

    // Session Errors (2000-2999)
    public const string SESSION_NOT_FOUND = "BFF_2000";
    public const string SESSION_EXPIRED = "BFF_2001";
    public const string SESSION_CREATION_FAILED = "BFF_2002";

    // Conversation Errors (3000-3999)
    public const string CONVERSATION_NOT_FOUND = "BFF_3000";
    public const string MESSAGE_SEND_FAILED = "BFF_3001";

    // External Service Errors (4000-4999)
    public const string CHATBOT_UNAVAILABLE = "BFF_4000";
    public const string FAQBOT_UNAVAILABLE = "BFF_4001";
    public const string SPEECH_SERVICE_UNAVAILABLE = "BFF_4002";
    public const string EXTERNAL_SERVICE_TIMEOUT = "BFF_4003";
    public const string EXTERNAL_SERVICE_ERROR = "BFF_4004";

    // Redis/Cache Errors (5000-5999)
    public const string REDIS_CONNECTION_FAILED = "BFF_5000";
    public const string CACHE_WRITE_FAILED = "BFF_5001";
    public const string CACHE_READ_FAILED = "BFF_5002";
}

namespace Chubb.Bot.AI.Assistant.Core.Constants;

public static class CacheKeys
{
    public const string SessionPrefix = "session:";
    public const string ConversationPrefix = "conversation:";
    public const string UserPrefix = "user:";

    public static string GetSessionKey(string sessionId) => $"{SessionPrefix}{sessionId}";
    public static string GetConversationKey(string conversationId) => $"{ConversationPrefix}{conversationId}";
    public static string GetUserSessionsKey(string userId) => $"{UserPrefix}{userId}:sessions";
}

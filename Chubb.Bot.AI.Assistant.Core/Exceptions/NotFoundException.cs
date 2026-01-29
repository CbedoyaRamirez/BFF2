namespace Chubb.Bot.AI.Assistant.Core.Exceptions;

public class NotFoundException : BusinessException
{
    public NotFoundException(string message)
        : base(message, "BFF_2000", 404)
    {
    }

    public NotFoundException(string entityName, string entityId, bool isEntityNotFound = true)
        : base($"{entityName} with ID '{entityId}' not found", "BFF_2000", 404)
    {
    }
}

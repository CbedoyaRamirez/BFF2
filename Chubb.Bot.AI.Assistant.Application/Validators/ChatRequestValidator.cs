using Chubb.Bot.AI.Assistant.Application.DTOs.Requests;
using FluentValidation;

namespace Chubb.Bot.AI.Assistant.Application.Validators;

public class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    public ChatRequestValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("SessionId is required");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(5000).WithMessage("Message cannot exceed 5000 characters");
    }
}

using Chubb.Bot.AI.Assistant.Application.DTOs.Requests;
using FluentValidation;

namespace Chubb.Bot.AI.Assistant.Application.Validators;

public class SessionRequestValidator : AbstractValidator<SessionRequest>
{
    public SessionRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");
    }
}

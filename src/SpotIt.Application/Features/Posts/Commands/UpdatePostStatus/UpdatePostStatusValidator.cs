using FluentValidation;
using SpotIt.Domain.Enums;

namespace SpotIt.Application.Features.Posts.Commands.UpdatePostStatus;

public class UpdatePostStatusValidator : AbstractValidator<UpdatePostStatusCommand>
{
    public UpdatePostStatusValidator()
    {
        RuleFor(x => x.PostId).NotEmpty();
        RuleFor(x => x.NewStatus).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(1000).When(x => x.Note is not null);
    }
}

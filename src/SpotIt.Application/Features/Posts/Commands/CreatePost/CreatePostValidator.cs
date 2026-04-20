using FluentValidation;

namespace SpotIt.Application.Features.Posts.Commands.CreatePost;

public class CreatePostValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.CategoryId).GreaterThan(0);
    }
}

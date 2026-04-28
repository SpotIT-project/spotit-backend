// ============================================================================
// AddCommentValidatorTests.cs — Unit tests for comment input validation
// ============================================================================

using FluentValidation.TestHelper;
using SpotIt.Application.Features.Comments.Commands.AddComment;
using Xunit;

namespace SpotIt.UnitTests.Validators;

public class AddCommentValidatorTests
{
    private readonly AddCommentValidator _sut = new();

    [Fact]
    public void Validate_EmptyContent_HasError()
    {
        var command = new AddCommentCommand(Guid.NewGuid(), "", false);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_EmptyPostId_HasError()
    {
        var command = new AddCommentCommand(Guid.Empty, "Valid content", false);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PostId);
    }

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        var command = new AddCommentCommand(Guid.NewGuid(), "Valid content", false);

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

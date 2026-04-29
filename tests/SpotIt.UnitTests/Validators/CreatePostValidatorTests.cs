// ============================================================================
// CreatePostValidatorTests.cs — Unit tests for input validation
// ============================================================================
// WHAT WE'RE TESTING:
//   FluentValidation rules. We ensure that bad inputs (empty strings, 
//   strings that are too long, invalid IDs) are caught before they ever reach 
//   the handler.
//
// HOW WE TEST IT:
//   We create a command with invalid data, run the validator, and assert that 
//   the result contains errors for specific properties.
// ============================================================================

using FluentValidation.TestHelper;
using SpotIt.Application.Features.Posts.Commands.CreatePost;
using Xunit;

namespace SpotIt.UnitTests.Validators;

public class CreatePostValidatorTests
{
    private readonly CreatePostValidator _sut = new(); // SUT = System Under Test

    [Fact]
    public void Validate_EmptyTitle_HasError()
    {
        // Arrange
        var command = new CreatePostCommand("", "Description", 1, false);

        // Act
        var result = _sut.TestValidate(command);

        // Assert — Using FluentValidation's TestHelper extension methods
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_TitleTooLong_HasError()
    {
        // 201 characters (limit is 200)
        var longTitle = new string('a', 201); 
        var command = new CreatePostCommand(longTitle, "Description", 1, false);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_EmptyDescription_HasError()
    {
        var command = new CreatePostCommand("Title", "", 1, false);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_InvalidCategoryId_HasError()
    {
        // CategoryId must be > 0
        var command = new CreatePostCommand("Title", "Description", 0, false);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        var command = new CreatePostCommand("Valid Title", "Valid Description", 1, false);

        var result = _sut.TestValidate(command);

        // Make sure no validation errors occurred
        result.ShouldNotHaveAnyValidationErrors();
    }
}

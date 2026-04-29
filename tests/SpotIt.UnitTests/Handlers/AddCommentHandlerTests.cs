// ============================================================================
// AddCommentHandlerTests.cs — Unit tests for adding a comment to a post
// ============================================================================
// WHAT WE'RE TESTING:
//   AddCommentHandler creates a Comment entity with data from the command
//   plus the current user's ID, then saves it via IUnitOfWork.
// ============================================================================

using NSubstitute;
using FluentAssertions;
using SpotIt.Application.Features.Comments.Commands.AddComment;
using SpotIt.Application.Interfaces;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;
using Xunit;

namespace SpotIt.UnitTests.Handlers;

public class AddCommentHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly AddCommentHandler _sut;

    public AddCommentHandlerTests()
    {
        _currentUser.UserId.Returns("commenter-1");
        _sut = new AddCommentHandler(_uow, _currentUser);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesComment()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var command = new AddCommentCommand(postId, "This is a test comment", false);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _uow.Comments.Received(1).AddAsync(
            Arg.Is<Comment>(c =>
                c.PostId == postId &&
                c.Content == "This is a test comment" &&
                c.IsOfficialResponse == false &&
                c.AuthorId == "commenter-1"),
            Arg.Any<CancellationToken>());

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetsCorrectAuthorAndPostId()
    {
        // Arrange — different user, official response
        _currentUser.UserId.Returns("employee-5");
        var handler = new AddCommentHandler(_uow, _currentUser);
        var postId = Guid.NewGuid();
        var command = new AddCommentCommand(postId, "Official response", true);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — verify the comment has the right author and is marked official
        await _uow.Comments.Received(1).AddAsync(
            Arg.Is<Comment>(c =>
                c.AuthorId == "employee-5" &&
                c.PostId == postId &&
                c.IsOfficialResponse == true),
            Arg.Any<CancellationToken>());
    }
}

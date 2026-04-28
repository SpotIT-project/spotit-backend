// ============================================================================
// LikePostHandlerTests.cs — Unit tests for the "like a post" feature
// ============================================================================
// WHAT WE'RE TESTING:
//   LikePostHandler checks if the user already liked the post.
//   - If NOT liked yet → creates a Like entity and saves
//   - If ALREADY liked → throws InvalidOperationException
//
// KEY CONCEPT: Testing with FindAsync
//   The handler uses FindAsync (returns a collection). We mock it to return
//   either an empty list (not liked) or a list with one Like (already liked).
// ============================================================================

using NSubstitute;
using FluentAssertions;
using SpotIt.Application.Features.Likes.Commands.LikePost;
using SpotIt.Application.Interfaces;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;
using System.Linq.Expressions;
using Xunit;

namespace SpotIt.UnitTests.Handlers;

public class LikePostHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly LikePostHandler _sut;

    public LikePostHandlerTests()
    {
        _currentUser.UserId.Returns("user-1");
        _sut = new LikePostHandler(_uow, _currentUser);
    }

    [Fact]
    public async Task Handle_NotYetLiked_CreatesLike()
    {
        // Arrange — FindAsync returns empty list (no existing like)
        var postId = Guid.NewGuid();
        _uow.Likes.FindAsync(Arg.Any<Expression<Func<Like, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<Like>());

        var command = new LikePostCommand(postId);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — a new Like should have been added
        await _uow.Likes.Received(1).AddAsync(
            Arg.Is<Like>(l => l.PostId == postId && l.UserId == "user-1"),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyLiked_ThrowsInvalidOperationException()
    {
        // Arrange — FindAsync returns a list with one Like (already liked)
        var postId = Guid.NewGuid();
        var existingLike = new Like { PostId = postId, UserId = "user-1" };
        _uow.Likes.FindAsync(Arg.Any<Expression<Func<Like, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Like> { existingLike });

        var command = new LikePostCommand(postId);

        // Act & Assert
        await FluentActions.Awaiting(() => _sut.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*already liked*");

        // Verify no Like was added and no save happened
        await _uow.Likes.DidNotReceive().AddAsync(Arg.Any<Like>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

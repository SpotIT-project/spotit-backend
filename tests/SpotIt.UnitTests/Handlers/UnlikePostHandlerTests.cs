// ============================================================================
// UnlikePostHandlerTests.cs — Unit tests for removing a like
// ============================================================================
// WHAT WE'RE TESTING:
//   UnLikePostHandler finds the user's Like for a post and removes it.
//   If the Like doesn't exist, it throws NotFoundException.
// ============================================================================

using NSubstitute;
using FluentAssertions;
using SpotIt.Application.Exceptions;
using SpotIt.Application.Features.Likes.Commands.UnlikePost;
using SpotIt.Application.Interfaces;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;
using System.Linq.Expressions;
using Xunit;

namespace SpotIt.UnitTests.Handlers;

public class UnlikePostHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly UnLikePostHandler _sut;

    public UnlikePostHandlerTests()
    {
        _currentUser.UserId.Returns("user-1");
        _sut = new UnLikePostHandler(_uow, _currentUser);
    }

    [Fact]
    public async Task Handle_LikeExists_RemovesLike()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var existingLike = new Like { PostId = postId, UserId = "user-1" };
        _uow.Likes.FindAsync(Arg.Any<Expression<Func<Like, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Like> { existingLike });

        // Act
        await _sut.Handle(new UnLikePostCommand(postId), CancellationToken.None);

        // Assert — the existing Like should have been removed
        _uow.Likes.Received(1).Remove(existingLike);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LikeNotFound_ThrowsNotFoundException()
    {
        // Arrange — FindAsync returns empty (user hasn't liked this post)
        _uow.Likes.FindAsync(Arg.Any<Expression<Func<Like, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<Like>());

        // Act & Assert
        await FluentActions.Awaiting(() => _sut.Handle(new UnLikePostCommand(Guid.NewGuid()), CancellationToken.None))
            .Should()
            .ThrowAsync<NotFoundException>();
    }
}

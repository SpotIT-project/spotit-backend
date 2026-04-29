// ============================================================================
// UpdatePostStatusHandlerTests.cs — Unit tests for changing a post's status
// ============================================================================
// WHAT WE'RE TESTING:
//   UpdatePostStatusHandler fetches a post by ID, changes its status,
//   creates a StatusHistory record, and saves. It throws NotFoundException
//   if the post doesn't exist.
//
// KEY TESTING CONCEPT: Testing exception paths
//   We test both the "happy path" (post exists → success) and the
//   "sad path" (post doesn't exist → throws NotFoundException).
//   Both paths are equally important in real apps.
// ============================================================================

using NSubstitute;
using FluentAssertions;
using SpotIt.Application.Exceptions;
using SpotIt.Application.Features.Posts.Commands.UpdatePostStatus;
using SpotIt.Application.Interfaces;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Enums;
using SpotIt.Domain.Interfaces;
using Xunit;

namespace SpotIt.UnitTests.Handlers;

public class UpdatePostStatusHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly UpdatePostStatusHandler _sut;

    public UpdatePostStatusHandlerTests()
    {
        _currentUser.UserId.Returns("employee-1");
        _sut = new UpdatePostStatusHandler(_uow, _currentUser);
    }

    [Fact]
    public async Task Handle_ExistingPost_UpdatesStatusAndCreatesHistory()
    {
        // Arrange — create a fake post that "exists" in the DB
        var postId = Guid.NewGuid();
        var existingPost = new Post
        {
            Id = postId,
            Title = "Broken light",
            Status = PostStatus.Pending,
            AuthorId = "citizen-1"
        };

        // Configure mock: when GetByIdAsync is called with this postId, return our fake post
        _uow.Posts.GetByIdAsync(postId, Arg.Any<CancellationToken>())
            .Returns(existingPost);

        var command = new UpdatePostStatusCommand(postId, PostStatus.InProgress, "Starting work");

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        // 1. The post's status should have been changed (the handler mutates the tracked entity)
        existingPost.Status.Should().Be(PostStatus.InProgress);

        // 2. A StatusHistory record should have been created with old → new status
        await _uow.StatusHistory.Received(1).AddAsync(
            Arg.Is<StatusHistory>(sh =>
                sh.PostId == postId &&
                sh.OldStatus == PostStatus.Pending &&
                sh.NewStatus == PostStatus.InProgress &&
                sh.Note == "Starting work" &&
                sh.ChangedByUserId == "employee-1"),
            Arg.Any<CancellationToken>());

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentPost_ThrowsNotFoundException()
    {
        // Arrange — configure mock to return null (post not found)
        var postId = Guid.NewGuid();
        _uow.Posts.GetByIdAsync(postId, Arg.Any<CancellationToken>())
            .Returns((Post?)null);

        var command = new UpdatePostStatusCommand(postId, PostStatus.Resolved, null);

        // Act & Assert — FluentAssertions' Awaiting().Should().ThrowAsync pattern
        // This is how you test that an async method throws a specific exception
        await FluentActions.Awaiting(() => _sut.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<NotFoundException>();

        // Verify we never tried to save (since we threw before that point)
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

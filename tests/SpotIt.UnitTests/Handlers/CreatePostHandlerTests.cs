// ============================================================================
// CreatePostHandlerTests.cs — Unit tests for the CreatePost CQRS handler
// ============================================================================
// WHAT WE'RE TESTING:
//   The CreatePostHandler takes a CreatePostCommand and:
//   1. Creates a new Post entity with data from the command
//   2. Creates a StatusHistory entry (initial "Pending" status)
//   3. Saves both to the database via IUnitOfWork
//
// HOW WE TEST IT:
//   We use NSubstitute to create mock (fake) implementations of IUnitOfWork and
//   ICurrentUserService. This lets us test the handler's LOGIC without needing
//   a real database. We verify that the handler calls the right methods with the
//   right arguments.
//
// KEY PATTERN: Arrange → Act → Assert
//   Arrange: set up mocks and test data
//   Act:     call the handler
//   Assert:  verify the expected behavior happened
// ============================================================================

using NSubstitute;
using FluentAssertions;
using SpotIt.Application.Features.Posts.Commands.CreatePost;
using SpotIt.Application.Interfaces;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;
using Xunit;

namespace SpotIt.UnitTests.Handlers;

public class CreatePostHandlerTests
{
    // These are our mock objects — fake implementations that we control
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly CreatePostHandler _sut; // SUT = System Under Test

    public CreatePostHandlerTests()
    {
        // Tell the mock: when someone reads .UserId, return this value
        _currentUser.UserId.Returns("test-user-id");
        _sut = new CreatePostHandler(_uow, _currentUser);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesPostAndStatusHistory()
    {
        // Arrange — create a command with test data
        var command = new CreatePostCommand("Pothole on Main St", "Large pothole near #42", 1, false);

        // Act — execute the handler
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — verify the handler did what we expect:
        // 1. It should have called Posts.AddAsync with a Post that matches our command
        await _uow.Posts.Received(1).AddAsync(
            Arg.Is<Post>(p =>
                p.Title == "Pothole on Main St" &&
                p.Description == "Large pothole near #42" &&
                p.CategoryId == 1 &&
                p.IsAnonymous == false &&
                p.AuthorId == "test-user-id"),
            Arg.Any<CancellationToken>());

        // 2. It should have created a StatusHistory entry for the initial Pending status
        await _uow.StatusHistory.Received(1).AddAsync(
            Arg.Is<StatusHistory>(sh => sh.NewStatus == Domain.Enums.PostStatus.Pending),
            Arg.Any<CancellationToken>());

        // 3. It should have saved changes exactly once
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        // 4. The returned GUID should not be empty (it's the new post's ID)
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_SetsCorrectAuthorId()
    {
        // Arrange — the current user is "user-abc"
        _currentUser.UserId.Returns("user-abc");
        var handler = new CreatePostHandler(_uow, _currentUser);
        var command = new CreatePostCommand("Title", "Description", 1, false);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — verify the Post's AuthorId matches the current user
        await _uow.Posts.Received(1).AddAsync(
            Arg.Is<Post>(p => p.AuthorId == "user-abc"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CallsSaveChangesExactlyOnce()
    {
        // This test verifies we don't accidentally save multiple times
        // (which would be a performance issue or could cause partial saves)
        var command = new CreatePostCommand("Title", "Desc", 1, true);

        await _sut.Handle(command, CancellationToken.None);

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

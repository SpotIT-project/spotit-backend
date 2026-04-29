// ============================================================================
// GetPostByIdHandlerTests.cs — Unit tests for fetching a single post
// ============================================================================
// WHAT WE'RE TESTING:
//   GetPostByIdHandler fetches a post with details (includes, navigation props),
//   maps it to a PostDto using AutoMapper, and returns it.
//   If not found → throws NotFoundException.
//
// KEY CONCEPT: Mocking IMapper
//   When unit-testing handlers that use AutoMapper, we mock IMapper to return
//   a predefined DTO. We're testing the HANDLER's logic, not AutoMapper's
//   mapping configuration (that would be a separate test).
// ============================================================================

using AutoMapper;
using NSubstitute;
using FluentAssertions;
using SpotIt.Application.DTOs;
using SpotIt.Application.Exceptions;
using SpotIt.Application.Features.Posts.Queries.GetPostById;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Enums;
using SpotIt.Domain.Interfaces;
using Xunit;

namespace SpotIt.UnitTests.Handlers;

public class GetPostByIdHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly GetPostByIdHandler _sut;

    public GetPostByIdHandlerTests()
    {
        _sut = new GetPostByIdHandler(_uow, _mapper);
    }

    [Fact]
    public async Task Handle_PostExists_ReturnsMappedDto()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = new Post
        {
            Id = postId,
            Title = "Broken streetlight",
            Status = PostStatus.Pending,
            AuthorId = "user-1",
            Category = new Category { Id = 1, Name = "Lighting" }
        };

        // The expected DTO that AutoMapper would produce
        var expectedDto = new PostDto
        {
            Id = postId,
            Title = "Broken streetlight",
            Status = PostStatus.Pending,
            CategoryName = "Lighting"
        };

        // Set up mocks
        _uow.Posts.GetByIdWithDetailsAsync(postId, Arg.Any<CancellationToken>())
            .Returns(post);
        _mapper.Map<PostDto>(post).Returns(expectedDto);

        // Act
        var result = await _sut.Handle(new GetPostByIdQuery(postId), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedDto);
    }

    [Fact]
    public async Task Handle_PostNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var postId = Guid.NewGuid();
        _uow.Posts.GetByIdWithDetailsAsync(postId, Arg.Any<CancellationToken>())
            .Returns((Post?)null);

        // Act & Assert
        await FluentActions.Awaiting(() => _sut.Handle(new GetPostByIdQuery(postId), CancellationToken.None))
            .Should()
            .ThrowAsync<NotFoundException>();
    }
}

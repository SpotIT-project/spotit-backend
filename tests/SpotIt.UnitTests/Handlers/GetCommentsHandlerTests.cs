// ============================================================================
// GetCommentsHandlerTests.cs — Unit tests for fetching comments by post
// ============================================================================

using AutoMapper;
using NSubstitute;
using FluentAssertions;
using SpotIt.Application.DTOs;
using SpotIt.Application.Features.Comments.Queries.GetComments;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;
using Xunit;

namespace SpotIt.UnitTests.Handlers;

public class GetCommentsHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly GetCommentsHandler _sut;

    public GetCommentsHandlerTests()
    {
        _sut = new GetCommentsHandler(_uow, _mapper);
    }

    [Fact]
    public async Task Handle_ReturnsCommentsForPost()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var comments = new List<Comment>
        {
            new() { Id = Guid.NewGuid(), PostId = postId, Content = "Comment 1" },
            new() { Id = Guid.NewGuid(), PostId = postId, Content = "Comment 2" }
        };
        var dtos = new List<CommentDto>
        {
            new() { Id = comments[0].Id, Content = "Comment 1" },
            new() { Id = comments[1].Id, Content = "Comment 2" }
        };

        _uow.Comments.GetByPostIdAsync(postId, Arg.Any<CancellationToken>())
            .Returns(comments);
        _mapper.Map<IEnumerable<CommentDto>>(comments)
            .Returns(dtos);

        // Act
        var result = await _sut.Handle(new GetCommentsQuery(postId), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(dtos);
    }
}

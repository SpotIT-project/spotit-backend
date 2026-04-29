// ============================================================================
// GetPostsHandlerTests.cs — Unit tests for paginated post listing
// ============================================================================
// WHAT WE'RE TESTING:
//   GetPostsHandler calls the repository's GetPagedAsync method and wraps
//   the results in a PagedResult<PostDto> via AutoMapper.
// ============================================================================

using AutoMapper;
using NSubstitute;
using FluentAssertions;
using SpotIt.Application.Common;
using SpotIt.Application.DTOs;
using SpotIt.Application.Features.Posts.Queries.GetPosts;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;
using Xunit;

namespace SpotIt.UnitTests.Handlers;

public class GetPostsHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly GetPostsHandler _sut;

    public GetPostsHandlerTests()
    {
        _sut = new GetPostsHandler(_uow, _mapper);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult()
    {
        // Arrange — simulate the repository returning 2 posts from a total of 5
        var posts = new List<Post>
        {
            new() { Id = Guid.NewGuid(), Title = "Post 1" },
            new() { Id = Guid.NewGuid(), Title = "Post 2" }
        };
        var dtos = new List<PostDto>
        {
            new() { Id = posts[0].Id, Title = "Post 1" },
            new() { Id = posts[1].Id, Title = "Post 2" }
        };

        // The repository returns a tuple: (Items, TotalCount)
        _uow.Posts.GetPagedAsync(1, 10, null, null, null, null, false, null, Arg.Any<CancellationToken>())
            .Returns((posts.AsEnumerable(), 5));

        _mapper.Map<IEnumerable<PostDto>>(Arg.Any<IEnumerable<Post>>())
            .Returns(dtos);

        var query = new GetPostsQuery(1, 10, null, null, null, null, false);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — verify the PagedResult wraps the data correctly
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }
}

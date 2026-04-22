using MediatR;
using SpotIt.Application.DTOs;

namespace SpotIt.Application.Features.Posts.Queries.GetPostById;

public record GetPostByIdQuery(Guid Id) : IRequest<PostDto>;

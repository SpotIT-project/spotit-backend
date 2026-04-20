using MediatR;
using SpotIt.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpotIt.Application.Features.Posts.Queries;

public record GetPostByIdQuery(Guid Id) : IRequest<PostDto>;

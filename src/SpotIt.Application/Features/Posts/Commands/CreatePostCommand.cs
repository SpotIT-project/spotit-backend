using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpotIt.Application.Features.Posts.Commands;

public record CreatePostCommand(string Title, string Description, int CategoryId, bool IsAnonymous) : IRequest<Guid>;


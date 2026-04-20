# SpotIt Phase 5 — Application Layer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the Application layer — DTOs, AutoMapper profile, pipeline behaviors, CQRS commands and queries with handlers and validators — wired into the API via a single `AddApplication()` extension.

**Architecture:** Application layer sits between Domain and Infrastructure. Controllers send MediatR messages (`IRequest<T>`). MediatR routes through the pipeline (LoggingBehavior → ValidationBehavior → Handler). Handlers use `IUnitOfWork` from Domain to interact with data. Entities never leave the Application layer — AutoMapper converts them to DTOs before returning to the controller.

**Tech Stack:** MediatR 14.1.0 · AutoMapper 16.1.1 · FluentValidation 12.1.1 · Microsoft.Extensions.Logging.Abstractions 10.0.5 (all already in Application.csproj)

**Teaching note:** Each task follows the 5-step loop — concept check first, student types code, review against expected output, Socratic follow-up, move on. Never give the code before the student attempts it.

---

## File Map

```
src/SpotIt.Application/
├── Common/
│   ├── Behaviors/
│   │   ├── LoggingBehavior.cs                    NEW
│   │   └── ValidationBehavior.cs                 NEW
│   ├── Mapping/
│   │   └── MappingProfile.cs                     NEW
│   └── PagedResult.cs                            NEW
├── Extensions/
│   └── ApplicationExtensions.cs                  NEW
├── Features/
│   └── Posts/
│       ├── Commands/
│       │   ├── CreatePost/
│       │   │   ├── CreatePostCommand.cs           NEW
│       │   │   ├── CreatePostHandler.cs           NEW
│       │   │   └── CreatePostValidator.cs         NEW
│       │   └── UpdatePostStatus/
│       │       ├── UpdatePostStatusCommand.cs     NEW
│       │       ├── UpdatePostStatusHandler.cs     NEW
│       │       └── UpdatePostStatusValidator.cs   NEW
│       ├── Queries/
│       │   ├── GetPosts/
│       │   │   ├── GetPostsQuery.cs               NEW
│       │   │   └── GetPostsHandler.cs             NEW
│       │   └── GetPostById/
│       │       ├── GetPostByIdQuery.cs            NEW
│       │       └── GetPostByIdHandler.cs          NEW
│       └── Dtos/
│           ├── PostDto.cs                         NEW
│           └── PostSummaryDto.cs                  NEW
└── SpotIt.Application.csproj                      MODIFY (add DI extensions)

src/SpotIt.Domain/
├── Interfaces/
│   └── IPostRepository.cs                        MODIFY (add GetByIdWithDetailsAsync)

src/SpotIt.Infrastructure/
├── Repositories/
│   └── PostRepository.cs                         MODIFY (implement GetByIdWithDetailsAsync)

src/SpotIt.API/
└── Program.cs                                    MODIFY (call AddApplication())
```

---

## Task 1: Extend IPostRepository with GetByIdWithDetailsAsync

The generic `GetByIdAsync` uses `FindAsync` — it doesn't eager-load navigation properties.
`GetPostByIdHandler` needs the full post (Author, Category, Likes, Comments) for the DTO.
The fix: add one method to `IPostRepository` and implement it in `PostRepository`.

**Files:**
- Modify: `src/SpotIt.Domain/Interfaces/IPostRepository.cs`
- Modify: `src/SpotIt.Infrastructure/Repositories/PostRepository.cs`

- [ ] **Step 1.1 — Concept check**

Ask Sava: "We need to return a full `PostDto` with AuthorName, CategoryName, LikeCount. The generic `GetByIdAsync` uses `FindAsync` which doesn't load navigation properties. Where should the eager-loading logic live — in the handler or in the repository? Why?"

Expected answer: In the repository. The handler belongs in Application and must not know about EF Core Include() — that's Infrastructure knowledge.

- [ ] **Step 1.2 — Student adds method to IPostRepository**

Target result in `src/SpotIt.Domain/Interfaces/IPostRepository.cs`:

```csharp
using SpotIt.Domain.Entities;
using SpotIt.Domain.Enums;

namespace SpotIt.Domain.Interfaces;

public interface IPostRepository : IRepository<Post>
{
    Task<(IEnumerable<Post> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        int? categoryId,
        PostStatus? status,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default);

    Task<Post?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
}
```

- [ ] **Step 1.3 — Student implements in PostRepository**

Read current `PostRepository.cs` first. Add after `GetPagedAsync`:

```csharp
public async Task<Post?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    => await _context.Posts
        .Include(p => p.Author)
        .Include(p => p.Category)
        .Include(p => p.Likes)
        .Include(p => p.Comments)
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == id, ct);
```

- [ ] **Step 1.4 — Build to verify**

```bash
dotnet build --no-restore
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 1.5 — Socratic follow-up**

Ask Sava: "Why do we use `AsNoTracking()` here but NOT in `CreatePostHandler` when we load a post to update its status?"

Expected answer: This is a read (Query) — we never save changes on this result, so tracking wastes memory. When updating status we need EF to track the entity so `Update()` and `SaveChangesAsync()` work.

---

## Task 2: PagedResult\<T\> DTO

A reusable wrapper for paginated responses — any list endpoint returns this, not a raw array.

**Files:**
- Create: `src/SpotIt.Application/Common/PagedResult.cs`

- [ ] **Step 2.1 — Concept check**

Ask Sava: "When a client asks for page 2 of posts, what information do they need beyond just the list of items? Why does the frontend need TotalCount?"

Expected answer: TotalCount to calculate how many pages exist and whether there's a next/previous page. Without it the frontend can't render pagination controls.

- [ ] **Step 2.2 — Student creates the file**

Target result in `src/SpotIt.Application/Common/PagedResult.cs`:

```csharp
namespace SpotIt.Application.Common;

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

- [ ] **Step 2.3 — Build to verify**

```bash
dotnet build --no-restore
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

---

## Task 3: Post DTOs

Two DTOs: `PostSummaryDto` for list views (lightweight), `PostDto` for single post detail (full).
Entities never leave the Application layer — these are what controllers and clients see.

**Files:**
- Create: `src/SpotIt.Application/Features/Posts/Dtos/PostSummaryDto.cs`
- Create: `src/SpotIt.Application/Features/Posts/Dtos/PostDto.cs`

- [ ] **Step 3.1 — Concept check**

Ask Sava: "Why do we have two separate DTOs instead of one? And why don't we just return the `Post` entity directly from the controller?"

Expected answers:
- Two DTOs: list pages only need lightweight data (title, status, like count) — loading full details for 20 posts at once wastes bandwidth and query time.
- No entity return: entities are EF Core objects with tracking info, navigation cycles (Post→Author→Posts→Author...), and internal fields callers shouldn't see.

- [ ] **Step 3.2 — Student creates PostSummaryDto**

Target result in `src/SpotIt.Application/Features/Posts/Dtos/PostSummaryDto.cs`:

```csharp
namespace SpotIt.Application.Features.Posts.Dtos;

public record PostSummaryDto(
    Guid Id,
    string Title,
    string Status,
    string CategoryName,
    int LikeCount,
    DateTime CreatedAt
);
```

- [ ] **Step 3.3 — Student creates PostDto**

Target result in `src/SpotIt.Application/Features/Posts/Dtos/PostDto.cs`:

```csharp
namespace SpotIt.Application.Features.Posts.Dtos;

public record PostDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string CategoryName,
    string? AuthorName,
    int LikeCount,
    int CommentCount,
    bool IsAnonymous,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
```

Note: `AuthorName` is nullable — null when `IsAnonymous = true`.

- [ ] **Step 3.4 — Build to verify**

```bash
dotnet build --no-restore
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

---

## Task 4: AutoMapper MappingProfile

AutoMapper maps `Post` → `PostDto` and `Post` → `PostSummaryDto` so handlers never do manual property assignment.

**Files:**
- Create: `src/SpotIt.Application/Common/Mapping/MappingProfile.cs`

- [ ] **Step 4.1 — Concept check**

Ask Sava: "AutoMapper needs to know how to map `Post.Status` (which is a `PostStatus` enum) to `PostDto.Status` (which is a `string`). Why can't it just figure this out automatically? What do we need to tell it?"

Expected answer: AutoMapper auto-maps properties with matching names and types. The types don't match here (enum vs string), so we need `.ForMember()` to tell it to call `.ToString()` on the enum.

- [ ] **Step 4.2 — Student creates MappingProfile**

Target result in `src/SpotIt.Application/Common/Mapping/MappingProfile.cs`:

```csharp
using AutoMapper;
using SpotIt.Application.Features.Posts.Dtos;
using SpotIt.Domain.Entities;

namespace SpotIt.Application.Common.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Post, PostSummaryDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
            .ForMember(d => d.LikeCount, o => o.MapFrom(s => s.Likes.Count));

        CreateMap<Post, PostDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
            .ForMember(d => d.AuthorName, o => o.MapFrom(s => s.IsAnonymous ? null : s.Author.FullName))
            .ForMember(d => d.LikeCount, o => o.MapFrom(s => s.Likes.Count))
            .ForMember(d => d.CommentCount, o => o.MapFrom(s => s.Comments.Count));
    }
}
```

- [ ] **Step 4.3 — Build to verify**

```bash
dotnet build --no-restore
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 4.4 — Socratic follow-up**

Ask Sava: "AutoMapper maps `s.Likes.Count` to `LikeCount`. What would happen at runtime if `Likes` is null because we forgot to eagerly load the collection?"

Expected answer: NullReferenceException at runtime. AutoMapper would try to call `.Count` on a null collection. This is why the repository must load the navigation properties before we map.

---

## Task 5: Pipeline Behaviors

Two behaviors wrap every handler: LoggingBehavior (outermost) times the request, ValidationBehavior (inner) runs FluentValidation before the handler executes.

**Files:**
- Create: `src/SpotIt.Application/Common/Behaviors/LoggingBehavior.cs`
- Create: `src/SpotIt.Application/Common/Behaviors/ValidationBehavior.cs`

- [ ] **Step 5.1 — Concept check**

Ask Sava: "Without pipeline behaviors, where would logging and validation live? What's the problem with that?"

Expected answer: In every handler — duplicated in every single one. Adding a new cross-cutting concern (e.g. performance tracking) would mean touching every handler. Behaviors are middleware for the MediatR pipeline: write once, apply everywhere.

- [ ] **Step 5.2 — Concept check: next() rule**

Ask Sava: "In a pipeline behavior, what happens if you call `next()` twice? What happens if you never call it?"

Expected answers:
- Twice: handler executes twice — double DB writes, duplicate data.
- Never: handler never runs, request returns default(TResponse) — silent failure.

- [ ] **Step 5.3 — Student creates LoggingBehavior**

Target result in `src/SpotIt.Application/Common/Behaviors/LoggingBehavior.cs`:

```csharp
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SpotIt.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        var response = await next();
        sw.Stop();
        logger.LogInformation("Handled {Request} in {Ms}ms", typeof(TRequest).Name, sw.ElapsedMilliseconds);
        return response;
    }
}
```

- [ ] **Step 5.4 — Student creates ValidationBehavior**

Target result in `src/SpotIt.Application/Common/Behaviors/ValidationBehavior.cs`:

```csharp
using FluentValidation;
using MediatR;

namespace SpotIt.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

- [ ] **Step 5.5 — Build to verify**

```bash
dotnet build --no-restore
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 5.6 — Socratic follow-up**

Ask Sava: "ValidationBehavior injects `IEnumerable<IValidator<TRequest>>`. What happens when a command has no validator registered? What does `validators.Any()` protect against?"

Expected answer: If no validator is registered for a request, the `IEnumerable` is empty — not null. Without the `Any()` check we'd still run the validation loop (doing nothing), which is harmless but wasteful. The early return is a minor optimization and makes intent clear.

---

## Task 6: CreatePost Command + Handler + Validator

The first full CQRS feature: citizen creates a post. Command carries the data, handler saves it + initial StatusHistory, validator enforces rules.

**Files:**
- Create: `src/SpotIt.Application/Features/Posts/Commands/CreatePost/CreatePostCommand.cs`
- Create: `src/SpotIt.Application/Features/Posts/Commands/CreatePost/CreatePostHandler.cs`
- Create: `src/SpotIt.Application/Features/Posts/Commands/CreatePost/CreatePostValidator.cs`

- [ ] **Step 6.1 — Concept check**

Ask Sava: "Why does `CreatePostCommand` carry a `UserId` field? Isn't the user already authenticated — shouldn't the handler know who is logged in?"

Expected answer: The handler is in Application layer — it has no access to `HttpContext` or ASP.NET Core. The controller reads the userId from the JWT claims and passes it as part of the command. Application stays framework-agnostic.

- [ ] **Step 6.2 — Student creates CreatePostCommand**

Target result in `src/SpotIt.Application/Features/Posts/Commands/CreatePost/CreatePostCommand.cs`:

```csharp
using MediatR;

namespace SpotIt.Application.Features.Posts.Commands.CreatePost;

public record CreatePostCommand(
    string Title,
    string Description,
    int CategoryId,
    bool IsAnonymous,
    string AuthorId
) : IRequest<Guid>;
```

- [ ] **Step 6.3 — Student creates CreatePostHandler**

Ask Sava to write the handler. Remind them: Post must be created AND a StatusHistory record must be created in the same `SaveChangesAsync` call — atomicity.

Target result in `src/SpotIt.Application/Features/Posts/Commands/CreatePost/CreatePostHandler.cs`:

```csharp
using MediatR;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Enums;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Posts.Commands.CreatePost;

public class CreatePostHandler(IUnitOfWork uow) : IRequestHandler<CreatePostCommand, Guid>
{
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken ct)
    {
        var post = new Post
        {
            Title = request.Title,
            Description = request.Description,
            CategoryId = request.CategoryId,
            IsAnonymous = request.IsAnonymous,
            AuthorId = request.AuthorId,
            Status = PostStatus.Pending
        };

        await uow.Posts.AddAsync(post, ct);

        var history = new StatusHistory
        {
            PostId = post.Id,
            OldStatus = PostStatus.Pending,
            NewStatus = PostStatus.Pending,
            Note = "Post submitted",
            ChangedByUserId = request.AuthorId
        };

        await uow.StatusHistory.AddAsync(history, ct);
        await uow.SaveChangesAsync(ct);
        return post.Id;
    }
}
```

- [ ] **Step 6.4 — Student creates CreatePostValidator**

Target result in `src/SpotIt.Application/Features/Posts/Commands/CreatePost/CreatePostValidator.cs`:

```csharp
using FluentValidation;

namespace SpotIt.Application.Features.Posts.Commands.CreatePost;

public class CreatePostValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(4000).WithMessage("Description cannot exceed 4000 characters.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("A valid category must be selected.");

        RuleFor(x => x.AuthorId)
            .NotEmpty().WithMessage("AuthorId is required.");
    }
}
```

- [ ] **Step 6.5 — Build to verify**

```bash
dotnet build --no-restore
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

---

## Task 7: UpdatePostStatus Command + Handler + Validator

City hall employees change a post's status (e.g. Pending → InProgress). Both the post and a new StatusHistory record must be updated atomically.

**Files:**
- Create: `src/SpotIt.Application/Features/Posts/Commands/UpdatePostStatus/UpdatePostStatusCommand.cs`
- Create: `src/SpotIt.Application/Features/Posts/Commands/UpdatePostStatus/UpdatePostStatusHandler.cs`
- Create: `src/SpotIt.Application/Features/Posts/Commands/UpdatePostStatus/UpdatePostStatusValidator.cs`

- [ ] **Step 7.1 — Concept check**

Ask Sava: "UpdatePostStatus needs to change `post.Status` AND insert a `StatusHistory` row. What would happen to the data if we called `SaveChangesAsync()` after the status update but before adding the StatusHistory?"

Expected answer: We'd have the new status saved but no audit trail for it — corrupt data. The history record exists to record every transition. Both must be in one `SaveChangesAsync()` or neither.

- [ ] **Step 7.2 — Student creates UpdatePostStatusCommand**

Target result in `src/SpotIt.Application/Features/Posts/Commands/UpdatePostStatus/UpdatePostStatusCommand.cs`:

```csharp
using MediatR;
using SpotIt.Domain.Enums;

namespace SpotIt.Application.Features.Posts.Commands.UpdatePostStatus;

public record UpdatePostStatusCommand(
    Guid PostId,
    PostStatus NewStatus,
    string? Note,
    string ChangedByUserId
) : IRequest<bool>;
```

- [ ] **Step 7.3 — Student creates UpdatePostStatusHandler**

Target result in `src/SpotIt.Application/Features/Posts/Commands/UpdatePostStatus/UpdatePostStatusHandler.cs`:

```csharp
using MediatR;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Posts.Commands.UpdatePostStatus;

public class UpdatePostStatusHandler(IUnitOfWork uow) : IRequestHandler<UpdatePostStatusCommand, bool>
{
    public async Task<bool> Handle(UpdatePostStatusCommand request, CancellationToken ct)
    {
        var post = await uow.Posts.GetByIdAsync(request.PostId, ct);
        if (post is null) return false;

        var history = new StatusHistory
        {
            PostId = post.Id,
            OldStatus = post.Status,
            NewStatus = request.NewStatus,
            Note = request.Note,
            ChangedByUserId = request.ChangedByUserId
        };

        post.Status = request.NewStatus;
        uow.Posts.Update(post);
        await uow.StatusHistory.AddAsync(history, ct);
        await uow.SaveChangesAsync(ct);
        return true;
    }
}
```

- [ ] **Step 7.4 — Student creates UpdatePostStatusValidator**

Target result in `src/SpotIt.Application/Features/Posts/Commands/UpdatePostStatus/UpdatePostStatusValidator.cs`:

```csharp
using FluentValidation;

namespace SpotIt.Application.Features.Posts.Commands.UpdatePostStatus;

public class UpdatePostStatusValidator : AbstractValidator<UpdatePostStatusCommand>
{
    public UpdatePostStatusValidator()
    {
        RuleFor(x => x.PostId).NotEmpty();
        RuleFor(x => x.ChangedByUserId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(1000).When(x => x.Note is not null);
    }
}
```

- [ ] **Step 7.5 — Build to verify**

```bash
dotnet build --no-restore
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

---

## Task 8: GetPosts Query + Handler

Returns a paginated, filtered list of posts. Read-only — no tracking, returns DTOs via AutoMapper.

**Files:**
- Create: `src/SpotIt.Application/Features/Posts/Queries/GetPosts/GetPostsQuery.cs`
- Create: `src/SpotIt.Application/Features/Posts/Queries/GetPosts/GetPostsHandler.cs`

- [ ] **Step 8.1 — Concept check**

Ask Sava: "This is a Query, not a Command. What are the two things that should always be true about a Query?"

Expected answer: It only reads data (no side effects, no DB writes). It uses `AsNoTracking()` — already handled by `GetPagedAsync` in the repository.

- [ ] **Step 8.2 — Student creates GetPostsQuery**

Target result in `src/SpotIt.Application/Features/Posts/Queries/GetPosts/GetPostsQuery.cs`:

```csharp
using MediatR;
using SpotIt.Application.Common;
using SpotIt.Application.Features.Posts.Dtos;
using SpotIt.Domain.Enums;

namespace SpotIt.Application.Features.Posts.Queries.GetPosts;

public record GetPostsQuery(
    int Page,
    int PageSize,
    int? CategoryId,
    PostStatus? Status,
    DateTime? From,
    DateTime? To
) : IRequest<PagedResult<PostSummaryDto>>;
```

- [ ] **Step 8.3 — Student creates GetPostsHandler**

Target result in `src/SpotIt.Application/Features/Posts/Queries/GetPosts/GetPostsHandler.cs`:

```csharp
using AutoMapper;
using MediatR;
using SpotIt.Application.Common;
using SpotIt.Application.Features.Posts.Dtos;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Posts.Queries.GetPosts;

public class GetPostsHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<GetPostsQuery, PagedResult<PostSummaryDto>>
{
    public async Task<PagedResult<PostSummaryDto>> Handle(GetPostsQuery request, CancellationToken ct)
    {
        var (items, totalCount) = await uow.Posts.GetPagedAsync(
            request.Page, request.PageSize, request.CategoryId,
            request.Status, request.From, request.To, ct);

        var dtos = mapper.Map<IEnumerable<PostSummaryDto>>(items);
        return new PagedResult<PostSummaryDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
```

- [ ] **Step 8.4 — Build to verify**

```bash
dotnet build --no-restore
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

---

## Task 9: GetPostById Query + Handler

Returns full post detail. Uses `GetByIdWithDetailsAsync` (added in Task 1) for eager loading.

**Files:**
- Create: `src/SpotIt.Application/Features/Posts/Queries/GetPostById/GetPostByIdQuery.cs`
- Create: `src/SpotIt.Application/Features/Posts/Queries/GetPostById/GetPostByIdHandler.cs`

- [ ] **Step 9.1 — Student creates GetPostByIdQuery**

Target result in `src/SpotIt.Application/Features/Posts/Queries/GetPostById/GetPostByIdQuery.cs`:

```csharp
using MediatR;
using SpotIt.Application.Features.Posts.Dtos;

namespace SpotIt.Application.Features.Posts.Queries.GetPostById;

public record GetPostByIdQuery(Guid PostId) : IRequest<PostDto?>;
```

- [ ] **Step 9.2 — Student creates GetPostByIdHandler**

Target result in `src/SpotIt.Application/Features/Posts/Queries/GetPostById/GetPostByIdHandler.cs`:

```csharp
using AutoMapper;
using MediatR;
using SpotIt.Application.Features.Posts.Dtos;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Posts.Queries.GetPostById;

public class GetPostByIdHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<GetPostByIdQuery, PostDto?>
{
    public async Task<PostDto?> Handle(GetPostByIdQuery request, CancellationToken ct)
    {
        var post = await uow.Posts.GetByIdWithDetailsAsync(request.PostId, ct);
        return post is null ? null : mapper.Map<PostDto>(post);
    }
}
```

- [ ] **Step 9.3 — Build to verify**

```bash
dotnet build --no-restore
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 9.4 — Socratic follow-up**

Ask Sava: "`GetPostByIdHandler` returns `PostDto?` — nullable. What should the controller do when the handler returns null? Why not throw an exception inside the handler instead?"

Expected answer: Controller returns `404 NotFound`. Handlers should not know about HTTP — they return null/false to signal "not found" and the controller decides how to translate that into an HTTP response. Throwing an HttpException in Application would couple it to ASP.NET Core.

---

## Task 10: ApplicationExtensions — Wire Everything Into DI

One extension method that registers MediatR, AutoMapper, FluentValidation validators, and both pipeline behaviors. Called from `Program.cs` with a single line.

**Files:**
- Modify: `src/SpotIt.Application/SpotIt.Application.csproj` (add FluentValidation.DependencyInjectionExtensions)
- Create: `src/SpotIt.Application/Extensions/ApplicationExtensions.cs`
- Modify: `src/SpotIt.API/Program.cs`

- [ ] **Step 10.1 — Add package to Application.csproj**

Add inside the existing `<ItemGroup>` with PackageReferences:

```xml
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.1.1" />
```

- [ ] **Step 10.2 — Concept check**

Ask Sava: "Pipeline behaviors are registered with `AddTransient`. Why TransientLifetime and not Scoped or Singleton? And why does registration ORDER matter for behaviors?"

Expected answers:
- Transient: behaviors are stateless (no shared state between requests), so the lifetime doesn't matter much — Transient is conventional.
- Order: MediatR executes behaviors in registration order. LoggingBehavior must wrap the entire operation including validation. If ValidationBehavior registers first, a failed validation would NOT be logged.

- [ ] **Step 10.3 — Student creates ApplicationExtensions**

Target result in `src/SpotIt.Application/Extensions/ApplicationExtensions.cs`:

```csharp
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SpotIt.Application.Common.Behaviors;

namespace SpotIt.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ApplicationExtensions).Assembly));

        services.AddAutoMapper(typeof(ApplicationExtensions).Assembly);

        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);

        // Registration order = execution order: Logging wraps Validation wraps Handler
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
```

- [ ] **Step 10.4 — Student adds AddApplication() to Program.cs**

Open `src/SpotIt.API/Program.cs`. Add before `AddIdentityServices()`:

```csharp
using SpotIt.Application.Extensions;
```

And in the service registrations, add:

```csharp
builder.Services.AddApplication();
```

Full registration order in Program.cs should be:

```csharp
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();           // MediatR + AutoMapper + FluentValidation + Behaviors
builder.Services.AddIdentityServices();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
```

- [ ] **Step 10.5 — Final build**

```bash
dotnet build
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 10.6 — Commit Phase 5**

```bash
git add src/SpotIt.Application/ src/SpotIt.Domain/Interfaces/IPostRepository.cs src/SpotIt.Infrastructure/Repositories/PostRepository.cs src/SpotIt.API/Program.cs
git commit -m "Phase 5(Complete): Application layer - CQRS handlers, FluentValidation, AutoMapper"
```

---

## Task 11: Phase 5 Knowledge Gate Test

Run this test before starting Phase 6. Sava must answer all four without looking anything up.

**Claude Code runs this test — do NOT move to Phase 6 until all pass.**

- [ ] **Question 1 (Explain):** "Explain the MediatR pipeline for a `CreatePostCommand` request — from the moment the controller calls `_mediator.Send()` to the moment data is saved. Name every step in order."

Expected answer covers: controller → mediator.Send() → LoggingBehavior (starts timer) → ValidationBehavior (runs CreatePostValidator, throws if invalid) → CreatePostHandler (creates Post + StatusHistory, calls SaveChangesAsync) → back through behaviors → controller gets Guid back.

- [ ] **Question 2 (What breaks):** "What breaks if we register ValidationBehavior before LoggingBehavior in DI? Give a concrete scenario."

Expected answer: A request that fails validation would throw before LoggingBehavior's `next()` is called. The request would never be logged — failed validations are invisible in the logs. You'd have no way to see that someone is hammering the API with bad requests.

- [ ] **Question 3 (Explain):** "Why does `GetPostByIdHandler` use `GetByIdWithDetailsAsync` instead of `GetByIdAsync`? What would go wrong with the regular one?"

Expected answer: `GetByIdAsync` uses `FindAsync` which doesn't eager-load navigation properties. AutoMapper tries to access `post.Author.FullName`, `post.Category.Name`, `post.Likes.Count` — all of which would be null (unloaded), causing a NullReferenceException at mapping time.

- [ ] **Question 4 (Write code):** "Write `GetCategoriesQuery` and its handler from scratch — it takes no parameters and returns `IEnumerable<CategoryDto>` where `CategoryDto` is a record with `Id` (int) and `Name` (string)."

Expected answer:
```csharp
// Query
public record GetCategoriesQuery() : IRequest<IEnumerable<CategoryDto>>;

// Handler
public class GetCategoriesHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<GetCategoriesQuery, IEnumerable<CategoryDto>>
{
    public async Task<IEnumerable<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var categories = await uow.Categories.GetAllAsync(ct);
        return mapper.Map<IEnumerable<CategoryDto>>(categories);
    }
}
```

- [ ] **Step 11.1 — Update knowledge-tracker.md**

After the gate test, update `knowledge-tracker.md`:

1. Move these from gaps to confirmed knowledge:
   - CQRS pattern: Command (write, returns Guid/bool, tracking ON) vs Query (read, returns DTO, AsNoTracking)
   - MediatR pipeline: `IRequest<T>` on message, `IRequestHandler<TRequest, T>` on handler, type link is the contract
   - `record` for Commands/Queries — immutable through pipeline
   - Pipeline Behaviors: LoggingBehavior → ValidationBehavior → Handler execution order
   - `next()` must be called exactly once — twice = duplicate handler execution
   - FluentValidation: `AbstractValidator<T>`, `RuleFor`, `WithMessage`, `When` for conditional rules
   - AutoMapper: `Profile`, `CreateMap`, `ForMember` + `MapFrom` for custom mappings
   - DTOs: why two DTOs (summary vs detail), why not return entities
   - Handler accesses userId from command (not from HttpContext — Application is framework-agnostic)
   - `GetByIdWithDetailsAsync` — repository-level eager loading for full detail queries

2. Update Phase 5 progress to ✅ Complete

3. Add Session 9/10 entry (whichever session this is) to the session log

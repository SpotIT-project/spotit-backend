# SpotIt — System Diagrams

Rendered automatically by GitHub. For local preview: VS Code + "Markdown Preview Mermaid Support" extension.

---

## 1. Use Case Diagram

> Mermaid has no native use-case type — this uses a flowchart approximation.
> For a formal UML use-case diagram paste the PlantUML block at the bottom into plantuml.com.

```mermaid
flowchart LR
    Citizen(["👤 Citizen"])
    Employee(["🏛️ City Hall Employee"])
    Admin(["🔑 Admin"])

    subgraph Public
        UC1["Register"]
        UC2["Login / Logout"]
    end

    subgraph Citizen Actions
        UC3["Browse & filter posts"]
        UC4["View post details"]
        UC5["Create a post"]
        UC6["Like / Unlike a post"]
        UC7["Comment on a post"]
        UC8["View my posts"]
    end

    subgraph Employee Actions
        UC9["Update post status"]
        UC10["Add official comment"]
    end

    subgraph Admin Actions
        UC11["Manage categories"]
        UC12["Manage user roles"]
        UC13["View analytics"]
    end

    Citizen --> UC1 & UC2 & UC3 & UC4 & UC5 & UC6 & UC7 & UC8
    Employee --> UC2 & UC3 & UC4 & UC9 & UC10
    Admin --> UC2 & UC3 & UC4 & UC9 & UC10 & UC11 & UC12 & UC13
```

<details>
<summary>PlantUML version (paste at plantuml.com for proper UML notation)</summary>

```plantuml
@startuml
left to right direction
skinparam packageStyle rectangle

actor Citizen
actor "City Hall Employee" as Employee
actor Admin

rectangle SpotIt {
  usecase "Register" as UC1
  usecase "Login / Logout" as UC2
  usecase "Browse & filter posts" as UC3
  usecase "View post details" as UC4
  usecase "Create a post" as UC5
  usecase "Like / Unlike a post" as UC6
  usecase "Comment on post" as UC7
  usecase "View my posts" as UC8
  usecase "Update post status" as UC9
  usecase "Add official comment" as UC10
  usecase "Manage categories" as UC11
  usecase "Manage user roles" as UC12
  usecase "View analytics" as UC13
}

Citizen --> UC1
Citizen --> UC2
Citizen --> UC3
Citizen --> UC4
Citizen --> UC5
Citizen --> UC6
Citizen --> UC7
Citizen --> UC8

Employee --> UC2
Employee --> UC3
Employee --> UC4
Employee --> UC9
Employee --> UC10

Admin --> UC2
Admin --> UC3
Admin --> UC4
Admin --> UC9
Admin --> UC10
Admin --> UC11
Admin --> UC12
Admin --> UC13
@enduml
```
</details>

---

## 2. Class Diagram

```mermaid
classDiagram
    class ApplicationUser {
        +string Id
        +string Email
        +string FullName
        +string City
        +string? ProfilePicture
        +DateTime CreatedAt
    }

    class Post {
        +Guid Id
        +string Title
        +string Description
        +int CategoryId
        +PostStatus Status
        +string AuthorId
        +bool IsAnonymous
        +DateTime CreatedAt
        +DateTime UpdatedAt
    }

    class Category {
        +int Id
        +string Name
        +string Description
        +string? IconUrl
        +string? AssignedEmployeeId
    }

    class Comment {
        +Guid Id
        +Guid PostId
        +string AuthorId
        +string Content
        +bool IsOfficialResponse
        +DateTime CreatedAt
    }

    class Like {
        +int Id
        +string UserId
        +Guid PostId
        +DateTime CreatedAt
    }

    class StatusHistory {
        +int Id
        +Guid PostId
        +string ChangedByUserId
        +PostStatus OldStatus
        +PostStatus NewStatus
        +string? Note
        +DateTime ChangedAt
    }

    class RefreshToken {
        +Guid Id
        +string Token
        +string UserId
        +DateTime ExpiresAt
        +DateTime CreatedAt
        +bool IsUsed
        +bool IsRevoked
    }

    class PostStatus {
        <<enumeration>>
        Pending
        UnderReview
        InProgress
        Resolved
        Rejected
    }

    ApplicationUser "1" --> "0..*" Post : authors
    ApplicationUser "1" --> "0..*" Like : places
    ApplicationUser "1" --> "0..*" Comment : writes
    ApplicationUser "1" --> "0..*" StatusHistory : changes status
    ApplicationUser "1" --> "0..*" RefreshToken : owns
    Category "1" --> "0..*" Post : categorizes
    Category "0..1" --> "0..1" ApplicationUser : assigned employee
    Post "1" --> "0..*" Like : receives
    Post "1" --> "0..*" Comment : has
    Post "1" --> "0..*" StatusHistory : tracked by
    Post --> PostStatus : has status
```

---

## 3. Sequence Diagrams

### 3a. Login Flow

```mermaid
sequenceDiagram
    actor User
    participant Browser
    participant AuthController
    participant UserManager
    participant JwtService
    participant Database

    User->>Browser: Fill email + password
    Browser->>AuthController: POST /api/auth/login
    AuthController->>UserManager: FindByEmailAsync(email)
    UserManager->>Database: SELECT * FROM users WHERE email = ?
    Database-->>UserManager: ApplicationUser
    UserManager-->>AuthController: user

    AuthController->>UserManager: CheckPasswordSignInAsync(user, password)
    UserManager-->>AuthController: SignInResult.Succeeded

    AuthController->>UserManager: GetRolesAsync(user)
    UserManager-->>AuthController: ["Citizen"]

    AuthController->>JwtService: GenerateAccessToken(user, roles)
    JwtService-->>AuthController: accessToken (JWT, 15 min)

    AuthController->>JwtService: GenerateRefreshToken()
    JwtService-->>AuthController: refreshToken (random string, 7 days)

    AuthController->>Database: INSERT INTO refresh_tokens
    AuthController->>Browser: Set-Cookie: accessToken + refreshToken (HttpOnly)
    AuthController-->>Browser: 200 { userId, email, fullName, role }
    Browser-->>User: Redirect to home feed
```

---

### 3b. Create Post Flow (with validation pipeline)

```mermaid
sequenceDiagram
    actor Citizen
    participant Browser
    participant PostsController
    participant MediatR
    participant ValidationBehavior
    participant CreatePostValidator
    participant CreatePostHandler
    participant UnitOfWork
    participant Database

    Citizen->>Browser: Fill title, description, category
    Browser->>PostsController: POST /api/posts (accessToken cookie)
    PostsController->>MediatR: Send(CreatePostCommand)

    MediatR->>ValidationBehavior: Handle (pipeline)
    ValidationBehavior->>CreatePostValidator: Validate(command)
    CreatePostValidator-->>ValidationBehavior: ValidationResult (pass)
    ValidationBehavior->>CreatePostHandler: next() — call handler

    CreatePostHandler->>CreatePostHandler: Id = Guid.CreateVersion7()
    CreatePostHandler->>CreatePostHandler: AuthorId = currentUser.UserId (from JWT)
    CreatePostHandler->>UnitOfWork: Posts.AddAsync(post)
    CreatePostHandler->>UnitOfWork: StatusHistory.AddAsync(history)
    CreatePostHandler->>UnitOfWork: SaveChangesAsync() — one transaction
    UnitOfWork->>Database: INSERT post + status_history (atomic)
    Database-->>UnitOfWork: OK

    CreatePostHandler-->>PostsController: Guid (new post id)
    PostsController-->>Browser: 201 Created
    Browser-->>Citizen: Show success + redirect to post
```

---

### 3c. Refresh Token Flow

```mermaid
sequenceDiagram
    actor Browser
    participant AuthController
    participant JwtService
    participant Database

    Note over Browser: accessToken expired, refreshToken still valid

    Browser->>AuthController: POST /api/auth/refresh (cookies sent automatically)
    AuthController->>AuthController: Read accessToken + refreshToken from cookies

    AuthController->>JwtService: GetPrincipalFromExpiredToken(accessToken)
    Note right of JwtService: ValidateLifetime = false — reads claims from expired token
    JwtService-->>AuthController: ClaimsPrincipal (userId extracted)

    AuthController->>Database: SELECT refresh_token WHERE token = ? AND userId = ?
    Database-->>AuthController: RefreshToken entity

    AuthController->>AuthController: Check IsUsed=false, IsRevoked=false, ExpiresAt > now
    AuthController->>Database: UPDATE refresh_token SET IsUsed = true

    AuthController->>JwtService: GenerateAccessToken(user, roles)
    AuthController->>JwtService: GenerateRefreshToken()
    AuthController->>Database: INSERT new refresh_token

    AuthController->>Browser: Set-Cookie: new accessToken + new refreshToken
    AuthController-->>Browser: 200 { message: "Token refreshed" }
```

---

### 3d. Update Post Status (Employee flow)

```mermaid
sequenceDiagram
    actor Employee
    participant Browser
    participant PostsController
    participant MediatR
    participant UpdateStatusHandler
    participant UnitOfWork
    participant Database

    Employee->>Browser: Select new status + add note
    Browser->>PostsController: PATCH /api/posts/{id}/status
    Note right of PostsController: [Authorize(Roles = "CityHallEmployee,Admin")]

    PostsController->>MediatR: Send(UpdatePostStatusCommand)
    MediatR->>UpdateStatusHandler: Handle

    UpdateStatusHandler->>UnitOfWork: Posts.GetByIdAsync(id)
    UnitOfWork->>Database: SELECT * FROM posts WHERE id = ?
    Database-->>UpdateStatusHandler: Post

    UpdateStatusHandler->>UpdateStatusHandler: post.Status = newStatus
    UpdateStatusHandler->>UnitOfWork: StatusHistory.AddAsync(history record)
    UpdateStatusHandler->>UnitOfWork: SaveChangesAsync()
    UnitOfWork->>Database: UPDATE post + INSERT status_history (atomic)

    UpdateStatusHandler-->>PostsController: Unit
    PostsController-->>Browser: 204 No Content
```

---

## 4. Architecture Diagram

```mermaid
flowchart TB
    subgraph Client["Client (Browser / React)"]
        React["React SPA"]
    end

    subgraph API["API Layer — SpotIt.API"]
        Controllers["Controllers\nAuthController · PostsController · CommentsController\nCategoriesController · AnalyticsController"]
        Middleware["ExceptionMiddleware\n(outermost — catches all exceptions)"]
        CUS["CurrentUserService\n(reads HttpContext.User claims)"]
    end

    subgraph Application["Application Layer — SpotIt.Application"]
        Handlers["MediatR Handlers\nCreatePost · GetPosts · GetPostById · UpdateStatus\nAddComment · GetComments · LikePost · UnlikePost\nGetCategories · GetPostsByStatus · GetTopCategories"]
        Behaviors["Pipeline Behaviors\nValidationBehavior → LoggingBehavior"]
        Validators["FluentValidation\nCreatePostValidator · AddCommentValidator · ..."]
        Mapper["AutoMapper\nMappingProfile"]
        AppInterfaces["Interfaces\nICurrentUserService · IJwtService · IUnitOfWork"]
    end

    subgraph Infrastructure["Infrastructure Layer — SpotIt.Infrastructure"]
        DbCtx["AppDbContext\n(EF Core 10)"]
        Repos["Repositories\nPostRepository · CommentRepository · Repository&lt;T&gt;"]
        UoW["UnitOfWork"]
        JWT["JwtService"]
        Seed["DatabaseSeeder"]
    end

    subgraph Domain["Domain Layer — SpotIt.Domain"]
        Entities["Entities\nPost · ApplicationUser · Category\nComment · Like · StatusHistory · RefreshToken"]
        Enums["Enums — PostStatus"]
        DomainIfaces["Interfaces\nIRepository&lt;T&gt; · IPostRepository · ICommentRepository · IUnitOfWork"]
    end

    subgraph External["External"]
        PG[("PostgreSQL")]
        Identity["ASP.NET Identity"]
    end

    React <-->|"HTTP (cookies)"| Controllers
    Controllers --> Middleware
    Controllers --> Handlers
    CUS -->|"implements"| AppInterfaces
    Handlers --> Behaviors
    Behaviors --> Validators
    Handlers --> Mapper
    Handlers --> AppInterfaces
    AppInterfaces -->|"implemented by"| Repos
    AppInterfaces -->|"implemented by"| JWT
    UoW --> Repos
    DbCtx --> PG
    DbCtx --> Identity
    Repos --> DbCtx

    style Domain fill:#e8f5e9,stroke:#388e3c
    style Application fill:#e3f2fd,stroke:#1976d2
    style Infrastructure fill:#fff3e0,stroke:#f57c00
    style API fill:#fce4ec,stroke:#c62828
```

---

## 5. Database Schema (ERD)

```mermaid
erDiagram
    ApplicationUser {
        string Id PK
        string UserName
        string Email
        string FullName
        string City
        string ProfilePicture "nullable"
        timestamptz CreatedAt
    }

    Post {
        uuid Id PK
        string Title "max 200"
        string Description "max 4000"
        int CategoryId FK
        string AuthorId FK
        string Status "post_status enum"
        bool IsAnonymous
        timestamptz CreatedAt
        timestamptz UpdatedAt
    }

    Category {
        int Id PK
        string Name
        string Description
        string IconUrl "nullable"
        string AssignedEmployeeId FK "nullable"
    }

    Comment {
        uuid Id PK
        uuid PostId FK
        string AuthorId FK
        string Content "max 2000"
        bool IsOfficialResponse
        timestamptz CreatedAt
    }

    Like {
        int Id PK
        string UserId FK
        uuid PostId FK
        timestamptz CreatedAt
    }

    StatusHistory {
        int Id PK
        uuid PostId FK
        string ChangedByUserId FK
        string OldStatus
        string NewStatus
        string Note "nullable"
        timestamptz ChangedAt
    }

    RefreshToken {
        uuid Id PK
        string Token
        string UserId FK
        timestamptz ExpiresAt
        timestamptz CreatedAt
        bool IsUsed
        bool IsRevoked
    }

    ApplicationUser ||--o{ Post : "authors (cascade delete)"
    ApplicationUser ||--o{ Like : "places (cascade delete)"
    ApplicationUser ||--o{ Comment : "writes"
    ApplicationUser ||--o{ StatusHistory : "changes"
    ApplicationUser ||--o{ RefreshToken : "owns"
    Category ||--o{ Post : "categorizes (restrict delete)"
    Category }o--o| ApplicationUser : "assigned employee"
    Post ||--o{ Like : "receives (cascade delete)\nUNIQUE(UserId, PostId)"
    Post ||--o{ Comment : "has (cascade delete)"
    Post ||--o{ StatusHistory : "audit trail"
```

---

## 6. Post Status — State Machine

```mermaid
stateDiagram-v2
    [*] --> Pending : POST /posts (citizen creates)

    Pending --> UnderReview : Employee begins review
    Pending --> Rejected : Employee rejects

    UnderReview --> InProgress : Work assigned
    UnderReview --> Rejected : Cannot proceed

    InProgress --> Resolved : Work completed
    InProgress --> Rejected : Cannot be resolved

    Resolved --> [*]
    Rejected --> [*]

    note right of Pending
        Every transition writes
        a StatusHistory record
    end note
```


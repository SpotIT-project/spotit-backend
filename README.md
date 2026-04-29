# SpotIt — Backend API

SpotIt is a civic engagement platform where citizens report urban problems (potholes, broken lights, etc.) and city hall employees track and respond to them.

**Stack:** ASP.NET Core 10 · EF Core 10 · PostgreSQL 17 · Clean Architecture · CQRS (MediatR) · FluentValidation · AutoMapper · JWT (HttpOnly cookies) · Docker

---

## Architecture

The project follows Clean Architecture with a strict inward dependency rule:

```
Domain → nothing
Application → Domain
Infrastructure → Application + Domain
API → Application + Infrastructure (DI wiring only)
```

```
src/
├── SpotIt.Domain/           # Entities, enums, repository interfaces
├── SpotIt.Application/      # CQRS handlers, validators, DTOs, MediatR pipeline
├── SpotIt.Infrastructure/   # EF Core, repositories, JwtService, file storage
└── SpotIt.API/              # Controllers, middleware, DI wiring, Program.cs

tests/
├── SpotIt.UnitTests/        # Handler and validator unit tests (NSubstitute + xUnit)
└── SpotIt.IntegrationTests/ # Full HTTP tests against a real PostgreSQL database
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/products/docker-desktop) (for PostgreSQL)

---

## Local Development Setup

### 1. Clone the repository

```bash
git clone https://github.com/SpotIT-project/spotit-backend.git
cd spotit-backend
```

### 2. Start the database

```bash
docker compose up -d db
```

This starts a PostgreSQL 17 container on port **5432**. Environment variables are read from `.env`.

Create a `.env` file in the project root (copy from the example below):

```env
POSTGRES_USER=spotit
POSTGRES_PASSWORD=yourpassword
POSTGRES_DB=spotitdb
PGADMIN_DEFAULT_EMAIL=admin@spotit.com
PGADMIN_DEFAULT_PASSWORD=admin

ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=spotitdb;Username=spotit;Password=yourpassword
Jwt__SecretKey=your-secret-key-at-least-32-characters-long
Jwt__Issuer=SpotIt.API
Jwt__Audience=SpotIt.Client
Jwt__ExpiryMinutes=15
```

### 3. Configure user secrets (alternative to .env for local dev)

```bash
cd src/SpotIt.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=spotitdb;Username=spotit;Password=yourpassword"
dotnet user-secrets set "Jwt:SecretKey" "your-secret-key-at-least-32-characters-long"
dotnet user-secrets set "Jwt:Issuer" "SpotIt.API"
dotnet user-secrets set "Jwt:Audience" "SpotIt.Client"
dotnet user-secrets set "Jwt:ExpiryMinutes" "15"
```

### 4. Run the API

```bash
dotnet run --project src/SpotIt.API
```

The API starts at `https://localhost:7xxx`. Swagger UI is available at `/swagger`.

> Migrations and seed data (roles, categories) are applied automatically on startup.

---

## Running with Docker Compose (Full Stack)

```bash
docker compose up --build
```

- API: `http://localhost:8080`
- pgAdmin: `http://localhost:5050`

---

## Production Deployment

Uses Docker Swarm secrets. The production compose file reads secrets from the host instead of environment variables:

```bash
# Create secrets
echo "yourpassword" | docker secret create db_password -
echo "Host=db;Port=5432;Database=spotitdb;Username=spotit;Password=yourpassword" | docker secret create connection_string -
echo "your-jwt-secret-at-least-32-chars" | docker secret create jwt_key -

# Deploy
docker compose -f docker-compose.prod.yml up -d
```

---

## Running Tests

### Unit Tests

No database required. Run directly:

```bash
dotnet test tests/SpotIt.UnitTests
```

### Integration Tests

Require a PostgreSQL instance on port **5433**. Start the test database first:

```bash
docker compose -f docker-compose.test.yml up -d
dotnet test tests/SpotIt.IntegrationTests
docker compose -f docker-compose.test.yml down
```

The test database uses an in-memory tmpfs volume — it is fast and disposable.

### CI

GitHub Actions runs on every push to `Sava` and every PR to `main`. It spins up a PostgreSQL 17 service and runs both unit and integration tests automatically.

---

## API Overview

Base URL: `https://localhost:7xxx/api`  
Authentication: HttpOnly cookies (`accessToken`, `refreshToken`) — no `Authorization` header needed.

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | `/auth/register` | Register a citizen account | Public |
| POST | `/auth/login` | Login, sets HttpOnly cookies | Public |
| POST | `/auth/refresh` | Rotate access + refresh tokens | Cookie |
| POST | `/auth/logout` | Clear auth cookies | Cookie |
| GET | `/auth/me` | Get current user profile | Cookie |
| GET | `/posts` | List posts (paginated, filterable, searchable) | Cookie |
| POST | `/posts` | Create a post | Cookie |
| GET | `/posts/{id}` | Get a single post | Cookie |
| DELETE | `/posts/{id}` | Delete a post (author only) | Cookie |
| PATCH | `/posts/{id}/status` | Update post status | Employee / Admin |
| POST | `/posts/{id}/photo` | Upload post photo (author only) | Cookie |
| POST | `/posts/{id}/likes` | Like a post | Cookie |
| DELETE | `/posts/{id}/likes` | Unlike a post | Cookie |
| GET | `/posts/{id}/comments` | Get comments on a post | Cookie |
| POST | `/posts/{id}/comments` | Add a comment | Cookie |
| GET | `/categories` | List all categories | Public |
| GET | `/admin/analytics/by-status` | Posts grouped by status | Admin |
| GET | `/admin/analytics/top-categories` | Top 5 categories by post count | Admin |

See [`docs/api-contract.md`](docs/api-contract.md) for full request/response shapes.

---

## Roles

| Role | How assigned |
|------|-------------|
| `Citizen` | Assigned automatically on register |
| `CityHallEmployee` | Assigned by an Admin |
| `Admin` | Seeded at startup |

---

## Post Status Flow

```
Pending → UnderReview → InProgress → Resolved
                      ↘              ↘
                       Rejected       Rejected
```

Every status transition is recorded in `StatusHistory` (audit trail).

---

## Error Responses

| Status | Trigger |
|--------|---------|
| 400 | Validation failure |
| 401 | Missing or expired access token |
| 403 | Authenticated but wrong role or not the resource owner |
| 404 | Resource not found |
| 409 | Conflict (e.g. duplicate like) |
| 500 | Unhandled server error |

---

## Key Design Decisions

- **CQRS via MediatR** — every feature is a Command or Query handler; controllers are thin
- **Pipeline behaviors** — `ValidationBehavior` runs FluentValidation before every handler; `LoggingBehavior` logs timing
- **HttpOnly cookies** — access tokens (15 min) and refresh tokens (7 days) are never exposed to JavaScript
- **UnitOfWork + Repository pattern** — EF Core is hidden behind interfaces; handlers depend on `IUnitOfWork`, not `AppDbContext`
- **Refresh token rotation** — every refresh invalidates the old token and issues a new one; replay attacks are rejected

---

## Project Docs

| File | Contents |
|------|----------|
| [`docs/api-contract.md`](docs/api-contract.md) | Full API reference |
| [`docs/diagrams.md`](docs/diagrams.md) | Architecture, class, sequence, ERD, and state-machine diagrams |
| [`docs/knowledge-tracker.md`](docs/knowledge-tracker.md) | Learning progress tracker |
| [`docs/concepts-review.md`](docs/concepts-review.md) | Concept reference notes |

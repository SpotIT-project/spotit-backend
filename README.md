# SpotIt — Backend API

SpotIt is a civic engagement platform where citizens report urban problems (potholes, broken lights, etc.) and city hall employees track and respond to them.

**Stack:** ASP.NET Core 10 · EF Core 10 · PostgreSQL 17 · Clean Architecture · CQRS (MediatR) · FluentValidation · AutoMapper · JWT (HttpOnly cookies) · Docker

---

## Quick Start (Run in 5 steps)

> **Before you begin — install these two things:**
>
> - [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) — check with `dotnet --version` (must show `10.x.x`)
> - [Docker Desktop](https://www.docker.com/products/docker-desktop) — check with `docker --version`, and make sure Docker Desktop is **open and running**

---

### Step 1 — Get the code

```bash
git clone https://github.com/SpotIT-project/spotit-backend.git
cd spotit-backend
```

---

### Step 2 — Create your `.env` file

Create a file named `.env` in the root of the project (same folder as `docker-compose.yml`).

Copy and paste this exactly — you can change the passwords if you want, but the defaults work fine for local dev:

```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=spotit

PGADMIN_DEFAULT_EMAIL=admin@spotit.com
PGADMIN_DEFAULT_PASSWORD=admin

ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=spotit;Username=postgres;Password=postgres
Jwt__SecretKey=your-super-secret-key-change-this-in-production-min-32-charsteresds
Jwt__Issuer=SpotIt.API
Jwt__Audience=SpotIt.Client
Jwt__ExpiryMinutes=15
```

---

### Step 3 — Create your `appsettings.Development.json` file

Create a file at this exact path: `src/SpotIt.API/appsettings.Development.json`

Copy and paste this (use the same credentials you put in `.env`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=spotit;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "SecretKey": "your-super-secret-key-change-this-in-production-min-32-charsteresds",
    "Issuer": "SpotIt.API",
    "Audience": "SpotIt.Client",
    "ExpiryMinutes": "60"
  }
}
```

> This file is gitignored on purpose — never commit real credentials.

---

### Step 4 — Start the database

```bash
docker compose up -d db
```

**Check it worked:** run `docker ps` — you should see a container named something like `spotit-backend-db-1` with status `healthy`.

---

### Step 5 — Run the API

```bash
dotnet run --project src/SpotIt.API
```

Wait a few seconds. When you see a line like:

```
Now listening on: https://localhost:7XXX
```

Open your browser and go to:

```
https://localhost:7XXX/scalar/v1
```

Replace `7XXX` with the actual port shown in the terminal. You'll see the interactive API explorer.

> The first run automatically creates all database tables and seeds test data (accounts, posts, categories). You don't need to run any migrations manually.

---

### Test accounts ready to use

| Role             | Email                | Password        |
| ---------------- | -------------------- | --------------- |
| Admin            | admin@spotit.ro      | `Admin@1234`    |
| CityHallEmployee | employee@spotit.ro   | `Employee@1234` |
| Citizen          | citizen@spotit.ro    | `Citizen@1234`  |

---

## Troubleshooting

**"Docker is not running" error**
Open Docker Desktop and wait for it to fully start, then retry.

**"Connection refused" or database error on startup**
Make sure the DB container is healthy: `docker ps`. If not, run `docker compose up -d db` again and wait ~10 seconds.

**Port already in use**
Something else is using port 5432. Stop it, or change the port in `.env` and `appsettings.Development.json` to `5433`.

**HTTPS certificate warning in browser**
Run `dotnet dev-certs https --trust` once, then restart the API.

**`appsettings.Development.json` not found error**
You skipped Step 3. Create the file as described above.

---

## Running Tests

### Unit Tests (no database needed)

```bash
dotnet test tests/SpotIt.UnitTests
```

### Integration Tests (needs a separate test database)

```bash
docker compose -f docker-compose.test.yml up -d
dotnet test tests/SpotIt.IntegrationTests
docker compose -f docker-compose.test.yml down
```

The test database runs on port **5433** (separate from your dev DB on 5432) and is wiped after each test run automatically.

---

## Run Everything with Docker (no local .NET install needed)

If you don't want to install .NET locally, you can run the full stack with Docker:

```bash
docker compose up --build
```

- API: `http://localhost:8080`
- pgAdmin (database UI): `http://localhost:5050`

> Note: this runs the API in a container. You still need Docker Desktop installed.

---

## Production Deployment (Docker Swarm)

```bash
# Create secrets
echo "yourpassword" | docker secret create db_password -
echo "Host=db;Port=5432;Database=spotit;Username=postgres;Password=yourpassword" | docker secret create connection_string -
echo "your-jwt-secret-at-least-32-chars" | docker secret create jwt_key -

# Deploy
docker compose -f docker-compose.prod.yml up -d
```

---

## API Overview

Base URL: `https://localhost:7xxx/api`
Authentication: HttpOnly cookies (`accessToken`, `refreshToken`) — no `Authorization` header needed after login.

| Method | Path                        | Description                                    | Auth             |
| ------ | --------------------------- | ---------------------------------------------- | ---------------- |
| POST   | `/auth/register`            | Register a citizen account                     | Public           |
| POST   | `/auth/login`               | Login, sets HttpOnly cookies                   | Public           |
| POST   | `/auth/refresh`             | Rotate access + refresh tokens                 | Cookie           |
| POST   | `/auth/logout`              | Clear auth cookies                             | Cookie           |
| GET    | `/auth/me`                  | Get current user profile                       | Cookie           |
| GET    | `/posts`                    | List posts (paginated, filterable, searchable) | Cookie           |
| POST   | `/posts`                    | Create a post                                  | Cookie           |
| GET    | `/posts/{id}`               | Get a single post                              | Cookie           |
| DELETE | `/posts/{id}`               | Delete a post (author only)                    | Cookie           |
| PATCH  | `/posts/{id}/status`        | Update post status                             | Employee / Admin |
| POST   | `/posts/{id}/photo`         | Upload post photo (author only)                | Cookie           |
| POST   | `/posts/{id}/likes`         | Like a post                                    | Cookie           |
| DELETE | `/posts/{id}/likes`         | Unlike a post                                  | Cookie           |
| GET    | `/posts/{id}/comments`      | Get comments on a post                         | Cookie           |
| POST   | `/posts/{id}/comments`      | Add a comment                                  | Cookie           |
| GET    | `/categories`               | List all categories                            | Public           |
| GET    | `/posts/{id}/history`       | Status change audit trail for a post           | Cookie           |
| GET    | `/analytics/by-status`      | Posts grouped by status                        | Employee / Admin |
| GET    | `/analytics/top-categories` | Top 5 categories by post count                 | Employee / Admin |

See [`docs/api-contract.md`](docs/api-contract.md) for full request/response shapes.

---

## Seed Data

On first startup the seeder creates test accounts and realistic mock posts automatically.

| Title                                      | Category  | Status      |
| ------------------------------------------ | --------- | ----------- |
| Large pothole on Republicii Street         | Roads     | InProgress  |
| Broken street light near Central Park      | Lighting  | Resolved    |
| Illegal trash dumping in Brătianu Park     | Waste     | Pending     |
| Water pipe burst on Independenței Ave      | Water     | UnderReview |
| Park benches completely destroyed          | Parks     | Rejected    |
| Missing road signs at roundabout           | Roads     | Pending     |

> Seed data is idempotent — restarting the app never creates duplicates.

---

## Roles

| Role               | How assigned                       |
| ------------------ | ---------------------------------- |
| `Citizen`          | Assigned automatically on register |
| `CityHallEmployee` | Assigned by an Admin               |
| `Admin`            | Seeded at startup                  |

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

| Status | Trigger                                                |
| ------ | ------------------------------------------------------ |
| 400    | Validation failure                                     |
| 401    | Missing or expired access token                        |
| 403    | Authenticated but wrong role or not the resource owner |
| 404    | Resource not found                                     |
| 409    | Conflict (e.g. duplicate like)                         |
| 500    | Unhandled server error                                 |

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

### Key Design Decisions

- **CQRS via MediatR** — every feature is a Command or Query handler; controllers are thin
- **Pipeline behaviors** — `ValidationBehavior` runs FluentValidation before every handler; `LoggingBehavior` logs timing
- **HttpOnly cookies** — access tokens (15 min) and refresh tokens (7 days) are never exposed to JavaScript
- **UnitOfWork + Repository pattern** — EF Core is hidden behind interfaces; handlers depend on `IUnitOfWork`, not `AppDbContext`
- **Refresh token rotation** — every refresh invalidates the old token and issues a new one; replay attacks are rejected

---

## CI

GitHub Actions runs on every push to `Sava` and every PR to `main`. It spins up a PostgreSQL 17 service container and runs both unit and integration tests automatically.

---

## Project Docs

| File                                           | Contents                                                       |
| ---------------------------------------------- | -------------------------------------------------------------- |
| [`docs/api-contract.md`](docs/api-contract.md) | Full API reference                                             |
| [`docs/diagrams.md`](docs/diagrams.md)         | Architecture, class, sequence, ERD, and state-machine diagrams |

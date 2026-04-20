# SpotIt Learning Plan — Design Spec

**Date:** 2026-04-11
**Student:** Sava — 3rd-year CS, Oradea Romania
**Goal:** Backend engineer capable of working at a company long-term and building own products

---

## Context

SpotIt is a civic engagement platform backend built with ASP.NET Core 10, Clean Architecture,
EF Core + PostgreSQL, JWT auth with HttpOnly cookies, CQRS with MediatR, FluentValidation,
AutoMapper, and Docker. Phases 1–4 are complete. This plan covers the remaining path to a
fully deployed, tested, production-grade backend.

---

## The Two Acts

**Act 1 — Backend Mastery** (Phases 5–11)
Build the remaining SpotIt backend. The gate condition: Sava can explain every layer of the
stack without looking anything up — from a request hitting the controller to data saved to
PostgreSQL and back. Knowledge gate tests enforce this before each phase transition.

**Act 2 — Ship It** (Phase 11)
Dockerize, set up CI/CD via GitHub Actions, deploy to a live server with a real URL and real
PostgreSQL. This is what separates engineers who can build from engineers who can build AND ship.

---

## Teaching Rhythm — 5-Step Loop Per Feature

Every concept and feature follows this loop without exception:

1. **Concept check** — Claude Code asks what Sava knows before explaining anything
2. **Student types** — no generated files; Sava writes the code
3. **Review + correct** — targeted feedback with explanation of why, not just what
4. **Socratic follow-up** — one question per concept to verify it clicked
5. **knowledge-tracker.md updated** — after every session, confirmed knowledge and gaps are updated

---

## Knowledge Gate Tests

At the end of each phase, before moving to the next, a gate test runs consisting of:

- 2x "explain this in your own words" questions
- 1x "what breaks if we remove X?" question
- 1x "write this from scratch" code challenge

**Gate rule:** Fail any one → drill session covers the gap → retest before next phase starts.
No exceptions. Fake understanding accumulates and causes real pain later.

---

## Phase Plan

| Phase | What Gets Built | Concepts Owned After |
|---|---|---|
| 1 ✅ | Solution scaffold — .sln, 4 projects, NuGet per layer, project references | Clean Architecture structure, dependency rule at compile time, FrameworkReference vs NuGet |
| 2 ✅ | Domain layer — entities, enums, IRepository, IPostRepository, IUnitOfWork | Entity design (Guid vs int, IdentityUser extension, nav properties), generic vs specific repo, IDisposable on UoW |
| 3 ✅ | Infrastructure — DbContext, Fluent API configs, EF migrations, Repositories, UoW | Fluent API vs data annotations, DeleteBehavior, AsNoTracking, GIN index, Repository pattern, UoW transaction boundary |
| 4 ✅ | JWT auth, refresh tokens, HttpOnly cookies, DatabaseSeeder | JWT 3-part structure, stateless validation, HttpOnly vs localStorage (XSS), refresh token rotation, replay attack prevention, ClockSkew, ValidateLifetime=false |
| 5 | Application layer — Commands, Queries, Handlers, Validators, AutoMapper profiles | CQRS pattern, MediatR pipeline + behaviors, FluentValidation rules, AutoMapper ProjectTo, DTO design |
| 6 | PostsController, CategoriesController, UsersController | Controller → MediatR flow, pagination response envelope, role-based auth with [Authorize], structured error responses |
| 7 | Status workflow, ranking algorithm, analytics endpoints | Business logic in handlers (not controllers), domain-meaningful queries, post ranking by likes + recency |
| 8 | Docker + Docker Compose | Container basics, multi-service compose (API + PostgreSQL), environment config, secrets management |
| 9 | Testing — integration + unit | WebApplicationFactory for integration tests, xUnit + NSubstitute for unit tests, mocking IUnitOfWork and repositories |
| 10 | Performance — caching + query optimization | IMemoryCache on read endpoints, EF Core query profiling, pagination at scale, N+1 problem identification |
| 11 | CI/CD + Deploy | GitHub Actions pipeline (build → test → deploy), VPS setup, HTTPS via Let's Encrypt, live PostgreSQL, real URL |

---

## Dual-Claude Testing System

Two tools, two distinct roles:

### Claude Code — Build Sessions
- Runs the 5-step teach-then-build loop during every session
- Runs the knowledge gate test at the end of each phase
- Updates `knowledge-tracker.md` after every session with:
  - Concepts confirmed
  - Gaps resolved
  - New gaps identified
  - Session log entry

### Claude.ai — Drill Sessions
- Sava pastes `knowledge-tracker.md` at the start of each drill session
- Used for deep concept drilling between build sessions
- No building — pure concept ownership and gap filling
- Triggered when: a concept didn't click during the build session, or a gate test was failed

### Suggested Weekly Rhythm (8+ hrs/week)

```
Monday / Wednesday   → Build session (Claude Code) — 2–3 hrs each
Friday               → Drill session (Claude.ai) — 1–2 hrs
Weekend (optional)   → Read docs, revisit knowledge-tracker.md
```

Phase gate test runs at the end of the last build session for that phase.
- Pass → next phase starts next session
- Fail → Friday drill covers gaps, retest Monday before continuing

---

## knowledge-tracker.md — Source of Truth

After every build session, `knowledge-tracker.md` is updated:
- Confirmed knowledge section updated (concepts added)
- Knowledge gaps table updated (resolved gaps struck through)
- Phase progress table updated
- Session log entry added with date, topics, bugs caught, assessment

The file is gitignored — personal to Sava, not part of the codebase.
When opening Claude.ai for a drill session, paste this one file — it contains everything.

---

## Success Criteria

**Act 1 complete when:**
- All SpotIt API endpoints are working and tested
- Sava can whiteboard the full request lifecycle without notes
- All Phase 1–10 gate tests passed

**Act 2 complete when:**
- SpotIt is live at a real URL
- CI/CD pipeline runs on every push to main
- PostgreSQL is running in production (not SQLite, not in-memory)
- HTTPS is configured

**Backend engineer ready when:**
- Can architect a Clean Architecture solution from scratch
- Can explain every design decision in SpotIt (why Guid, why UoW, why HttpOnly, etc.)
- Has a live deployed project to show in interviews or use as a product foundation

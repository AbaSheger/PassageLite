# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PassageLite is a .NET 8 Web API implementing a physical access control system with JWT authentication, role-based authorization, and PostgreSQL persistence. It is a portfolio project showcasing Clean Architecture.

## Common Commands

### Build & Run
```bash
# Docker Compose (recommended — starts API + PostgreSQL)
docker compose up --build
# API at http://localhost:5000, Swagger at http://localhost:5000/swagger

# Local development (requires PostgreSQL on localhost:5432)
dotnet build
cd src/PassageLite.Api && dotnet run
```

### Tests
```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Database Migrations
```bash
# Add migration (run from src/PassageLite.Api/)
dotnet ef migrations add <MigrationName> -p ../PassageLite.Infrastructure -o Data/Migrations

# Apply migrations manually
dotnet ef database update -p ../PassageLite.Infrastructure
```
Migrations also run automatically on startup in the Docker environment.

## Architecture

Clean Architecture with four layers:

- **Domain** (`PassageLite.Domain`): Core entities (`User`, `Area`, `AccessGrant`) and repository interfaces. No dependencies on other layers.
- **Application** (`PassageLite.Application`): Service interfaces + implementations, DTOs. Depends only on Domain.
- **Infrastructure** (`PassageLite.Infrastructure`): EF Core `AppDbContext`, repository implementations, `UnitOfWork`, `DbSeeder`. Depends on Domain.
- **API** (`PassageLite.Api`): Controllers, `Program.cs` (DI wiring, middleware), Swagger, JWT config. Depends on Application + Infrastructure.

### Request Flow
```
HTTP → Controller → IService → IUnitOfWork → IRepository → EF Core → PostgreSQL
```

### Key Patterns
- **Unit of Work** (`IUnitOfWork`): Single entry point to all repositories; call `SaveChangesAsync()` to commit.
- **Repository Pattern**: `IUserRepository`, `IAreaRepository`, `IAccessGrantRepository` abstract data access.
- **JWT Auth**: `JwtService` generates tokens with `Sub` (user ID), `Email`, and `Role` claims. Expiration defaults to 60 minutes.

### Domain Logic
`AccessGrant.IsCurrentlyValid()` is the core business rule — it checks `ValidFrom`, `ValidTo`, and `IsRevoked` to determine whether a grant permits access at the current moment.

### Seeded Demo Data
| Email | Password | Role |
|---|---|---|
| admin@passage.local | Admin123! | Admin |
| user@passage.local | User123! | User |

Areas: "Main Lobby", "Server Room"

## API Endpoints

| Method | Path | Auth |
|---|---|---|
| POST | `/auth/login` | None |
| GET | `/auth/me` | JWT |
| GET | `/areas` | JWT |
| POST | `/areas` | Admin |
| POST | `/access/grant` | Admin |
| POST | `/access/revoke` | Admin |
| GET | `/access/my` | JWT |
| GET | `/access/check?areaId=...` | JWT |
| GET | `/health` | None |

## Tests

Located in `tests/PassageLite.Tests/`. Two test files:
- **`AccessServiceTests.cs`**: Unit tests using EF Core in-memory database, covering the grant validity business logic.
- **`IntegrationTests.cs`**: End-to-end tests using `WebApplicationFactory` against the real service stack.

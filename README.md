# GearUp

GearUp is a modular backend built on ASP.NET Core with Clean Architecture principles. It focuses on scalable API design, role-based access control, operational safety for database tasks, and production-friendly infrastructure (Docker, Redis, health checks, structured logging, and OpenTelemetry).

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Repository Structure](#repository-structure)
- [Configuration](#configuration)
- [Database and Seeding Tasks](#database-and-seeding-tasks)
- [API and Realtime](#api-and-realtime)
- [Observability and Runtime Features](#observability-and-runtime-features)
- [Testing](#testing)
- [Deployment (Docker Compose)](#deployment-docker-compose)
- [Contributing](#contributing)
- [License](#license)

## Overview

What GearUp includes today:

- Clean Architecture split across `Presentation`, `Application`, `Domain`, and `Infrastructure` projects
- JWT authentication with access/refresh token flows
- Role/policy authorization (`AdminOnly`, `DealerOnly`, `CustomerOnly`)
- REST APIs under `/api/v1/*`
- SignalR hubs for post, notification, and chat updates
- PostgreSQL via EF Core (with pooled DbContext + retry policies)
- Redis caching and Redis health checks
- Swagger/OpenAPI in Development
- Serilog + OpenTelemetry instrumentation

## Quick Start

### Prerequisites

- .NET SDK 9.x (see `global.json`)
- Docker + Docker Compose (optional, for containerized run)
- PostgreSQL 16.x (if running locally without Docker)
- Redis (if running locally without Docker)

### 1) Clone and enter repository

```bash
git clone https://github.com/yourusername/GearUp.git
cd GearUp
```

### 2) Create `.env`

The app loads `.env` from either the current directory or one level above at startup.

```env
ASPNETCORE_ENVIRONMENT=Development

ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=gearup;Username=postgres;Password=your_password
Redis__ConnectionString=localhost:6379

Jwt__Issuer=your_issuer
Jwt__Audience=your_audience
Jwt__AccessToken_SecretKey=your_access_secret
Jwt__EmailVerificationToken_SecretKey=your_email_verification_secret
# Optional but recommended:
# Jwt__OpaqueTokenPepper=your_separate_pepper

BREVO_API_KEY=your_brevo_key
# Alternative legacy key supported by config lookup:
# SendGridApiKey=your_brevo_key

FromEmail=no-reply@example.com
ClientUrl=http://localhost:3000
CLOUDINARY_URL=cloudinary://<api_key>:<api_secret>@<cloud_name>

# Used by admin seeding flows (`APP_STARTUP_MODE=db-seed` or legacy `db-task`)
ADMIN_USERNAME=admin
ADMIN_EMAIL=admin@example.com
ADMIN_PASSWORD=your_secure_password

# Optional startup DB task flags
RUN_DB_TASKS_IN_DEVELOPMENT=false
RUN_DB_TASKS_ONCE_AND_EXIT=false

# Optional when startup mode runs seed tasks
# full (default in non-prod) or admin (default in prod)
DB_SEED_SCOPE=full
```

### 3) Run with Docker Compose

```bash
docker compose up --build
```

Default mapped ports from `docker-compose.yml`:

- API: `http://localhost:5255`
- Swagger UI (Development): `http://localhost:5255`
- Redis: `localhost:6379`
- Aspire dashboard: `http://localhost:18888`

### 4) Run locally (without Docker)

```bash
dotnet restore
dotnet ef database update --project GearUp.Infrastructure --startup-project GearUp.Presentation
dotnet run --project GearUp.Presentation
```

## Architecture

GearUp follows a layered architecture:

```text
GearUp
├── GearUp.Presentation    # API controllers, middleware, DI, host startup
├── GearUp.Application     # Use-cases, service contracts, DTOs, validators
├── GearUp.Domain          # Entities, enums, domain-level rules
├── GearUp.Infrastructure  # EF Core, repositories, external providers
└── GearUp.UnitTests       # Unit test project
```

## Repository Structure

Key folders and files:

- `GearUp.sln` - solution file
- `GearUp.Presentation/Program.cs` - app startup pipeline, DB task flags, middleware, hub mapping
- `GearUp.Presentation/Extensions/ServiceExtensions.cs` - DI setup for auth, Redis, versioning, health checks, OpenTelemetry
- `GearUp.Infrastructure/DependencyInjection.cs` - PostgreSQL EF Core setup with context pooling/retries
- `GearUp.Presentation/docs/API_ENDPOINTS.md` - endpoint and SignalR contract reference
- `docker-compose.yml` - PostgreSQL + API + Redis + Aspire dashboard services
- `CONTRIBUTING.md` - contribution workflow and standards

## Configuration

`ServiceExtensions.AddServices(...)` validates required runtime settings and throws early if critical values are missing.

Important settings:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessToken_SecretKey`, `Jwt__EmailVerificationToken_SecretKey`
- `BREVO_API_KEY` (or `SendGridApiKey` fallback)
- `FromEmail`, `ClientUrl`, `CLOUDINARY_URL`
- `Redis__ConnectionString` (or `Redis:ConnectionString` / `REDIS_URL`)
- `Cors__AllowedOrigins` (or `CORS_ALLOWED_ORIGINS`) for production frontend origins (comma-separated)

### Render notes

- Health check path: `/health`
- Container listens on `8080` (`Dockerfile` sets `ASPNETCORE_URLS=http://+:8080`)
- Use `render.yaml` as a deployment blueprint (`gearup-api` web service)
- Keep `APP_STARTUP_MODE=web` on the web service
- For one-off production migration run, use `APP_STARTUP_MODE=db-migrate`, then switch back to `web`
- For one-off migration+seed run (non-production recommended), use `APP_STARTUP_MODE=db-seed`, then switch back to `web`

### Render env checklist

Set these on Render before first production deploy:

- `APP_STARTUP_MODE=web`
- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection`
- `Redis__ConnectionString`
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessToken_SecretKey`, `Jwt__EmailVerificationToken_SecretKey`
- `Jwt__OpaqueTokenPepper` (recommended in production)
- `BREVO_API_KEY`, `FromEmail`, `ClientUrl`, `CLOUDINARY_URL`
- `CORS_ALLOWED_ORIGINS` (comma-separated, e.g. `https://app.example.com,https://admin.example.com`)
- `ADMIN_USERNAME`, `ADMIN_EMAIL`, `ADMIN_PASSWORD` (required only when `APP_STARTUP_MODE=db-seed` or legacy `db-task`)

## Database and Seeding Tasks

### Migrations

```bash
dotnet ef migrations add <MigrationName> --project GearUp.Infrastructure --startup-project GearUp.Presentation
dotnet ef database update --project GearUp.Infrastructure --startup-project GearUp.Presentation
```

### Startup DB behavior

Database migrate/seed is controlled by startup mode in `Program.cs`:

1. `APP_STARTUP_MODE=web` (default)
   - Starts the API only (no migrate/seed)
2. `APP_STARTUP_MODE=db-migrate`
   - Runs migrations only, then exits without starting the web host
3. `APP_STARTUP_MODE=db-seed`
   - Runs migrations + seed tasks, then exits without starting the web host
   - Seeding honors `DB_SEED_SCOPE` (`admin` or `full`)
4. `APP_STARTUP_MODE=db-task` (legacy alias)
   - Backward-compatible alias for `db-seed`

Legacy dev-only flags remain supported for local workflows:

- `RUN_DB_TASKS_IN_DEVELOPMENT=true`
- `RUN_DB_TASKS_ONCE_AND_EXIT=true`

Outside Development, legacy flags are ignored to avoid accidental production shutdown/startup-task runs. In production, prefer `APP_STARTUP_MODE=db-migrate` for schema updates.

### EF Core resiliency setup

`GearUp.Infrastructure/DependencyInjection.cs` configures:

- `AddDbContextPool<GearUpDbContext>(poolSize: 128)`
- `EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: 10s)`
- `CommandTimeout(60)`

## API and Realtime

### REST API

- Base versioned route style: `/api/v1/...`
- API versioning is enabled and reports supported versions in headers

### Swagger / OpenAPI

- Swagger is enabled in Development
- UI route is configured at app root (`/`) via `RoutePrefix = string.Empty`
- Raw doc endpoint remains `/swagger/v1/swagger.json`

Export OpenAPI (example):

```bash
dotnet build GearUp.Presentation -c Debug
dotnet swagger tofile --output swagger.json GearUp.Presentation/bin/Debug/net9.0/GearUp.Presentation.dll v1
```

### SignalR hubs

Mapped in `Program.cs`:

- `/hubs/post`
- `/hubs/notification`
- `/hubs/chat`

For endpoint-level reference, see `GearUp.Presentation/docs/API_ENDPOINTS.md`.

## Observability and Runtime Features

- Health endpoint: `GET /health`
  - Includes DB (`AddDbContextCheck`) and Redis checks
- Global exception middleware: `GearUp.Presentation/Middlewares/ExceptionMiddleware.cs`
- Serilog request logging: `app.UseSerilogRequestLogging()`
- OpenTelemetry tracing + metrics configured in `ServiceExtensions.cs`
- Fixed-window rate limiter policy (`"Fixed"`) enabled via `app.UseRateLimiter()`

## Testing

Run all tests:

```bash
dotnet test
```

Run only the unit test project:

```bash
dotnet test GearUp.UnitTests/GearUp.UnitTests.csproj
```

## Deployment (Docker Compose)

The provided `docker-compose.yml` brings up:

- `db` (PostgreSQL 16)
- `redis`
- `aspire-dashboard`
- `api` (this application)

Start services:

```bash
docker compose up -d --build
```

Stop services:

```bash
docker compose down
```

## Contributing

See `CONTRIBUTING.md` for coding standards, commit conventions, and PR checklist.

## License

MIT License.

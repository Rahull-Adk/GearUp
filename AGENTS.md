# AGENTS.md

## Scope
- This guide applies to the `GearUp/` solution (`GearUp.sln`) with 4 runtime layers: `GearUp.Presentation`, `GearUp.Application`, `GearUp.Domain`, `GearUp.Infrastructure` plus `GearUp.UnitTests`.
- Prefer code truth over docs: several README sections still reference `GearUp.API`, but the executable project is `GearUp.Presentation`.

## Architecture and request flow
- Request path is Controller -> Application Service -> Repository/Helper -> EF Core (`GearUpDbContext`) -> `Result<T>` -> `ResponseMapper.ToApiResponse`.
- Example login chain: `GearUp.Presentation/Controllers/AuthController.cs` -> `GearUp.Application/Services/Auth/LoginService.cs` -> `GearUp.Infrastructure/Repositories/UserRepository.cs`.
- Keep controllers thin; business logic and validation live in services/validators (see `AuthController` vs `LoginService`).
- Domain entities use factory/mutator methods (for example `User.CreateLocalUser`, `User.SetPassword`) instead of public setters.
- `Program.cs` runs `db.Database.Migrate()` and seeds admin/default data at startup (`AdminSeeder`, `DbSeeder`).

## Dependency wiring and conventions
- Most DI lives in `GearUp.Presentation/Extensions/ServiceExtensions.cs`; infra-only wiring lives in `GearUp.Infrastructure/DependencyInjection.cs`.
- Services and repos are manually registered as scoped; AutoMapper profiles are manually added and validated at startup.
- Validation is FluentValidation injected per DTO (example: `RegisterRequestDtoValidator` registered as `IValidator<RegisterRequestDto>`).
- API responses should continue using `Result<T>` from Application and map in Presentation via `ToApiResponse()`.
- Auth claim conventions are project-specific: user id is claim `"id"`; role policies are `CustomerOnly`, `DealerOnly`, `AdminOnly`.

## Realtime and integration points
- SignalR hubs are mapped in `Program.cs`: `/hubs/post`, `/hubs/notification`, `/hubs/chat`.
- JWT for hubs is passed via query `access_token` (configured in `AddJwtBearer` event in `ServiceExtensions.cs`).
- Realtime event fanout is abstracted through `IRealTimeNotifier` -> `SignalRRealTimeNotifier`.
- External providers: MySQL (Pomelo EF), Redis cache, Brevo transactional email (`BREVO_API_KEY`), Cloudinary (`CLOUDINARY_URL`), OpenTelemetry OTLP.
- Health endpoint is `/health` and checks both DB + Redis.

## Local workflow (verified from repo config)
- SDK is pinned via `global.json` to .NET `9.0.306`; target frameworks are `net9.0`.
- Environment variables are loaded from repo-root `.env` in `Program.cs`; start from `.env.example`.
- Preferred container flow: `docker-compose.yml` (API on `5255`, MySQL on `3307`, Redis on `6379`, Aspire dashboard on `18888`).
- Non-container flow typically needs migration first, then running `GearUp.Presentation`.
- Tests use xUnit + Moq in `GearUp.UnitTests`; run solution tests before finalizing changes.

## Project-specific gotchas
- Use `DateTime.UtcNow` patterns already used across services/entities.
- Keep CORS-friendly frontend origins (`http://localhost:3000` and `http://localhost:5173`) unless intentionally changing frontend contracts.
- Be careful with env key naming drift in docs (`SendGridApiKey`) vs runtime code (`BREVO_API_KEY` in `ServiceExtensions.cs`).
- Preserve existing `Result<T>` status/message semantics because controllers return `StatusCode(result.Status, ...)` directly.


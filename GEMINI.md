# GearUp - Project Context

GearUp is a modular backend built on ASP.NET Core following Clean Architecture principles. It provides a scalable API for a car-related platform, featuring role-based access control, real-time updates via SignalR, and robust infrastructure.

## Project Architecture

The solution is divided into the following layers:

- **GearUp.Domain**: Contains core domain entities, enums, and rules. No dependencies on other projects.
- **GearUp.Application**: Defines service interfaces, use cases, DTOs, and validators (FluentValidation). Depends only on `GearUp.Domain`.
- **GearUp.Infrastructure**: Implements persistence (EF Core with PostgreSQL), messaging (RabbitMQ), SignalR hubs, and external service integrations (Brevo for email, Cloudinary for images). Depends on `GearUp.Application`.
- **GearUp.Presentation**: The entry point (ASP.NET Core Web API). Contains controllers, middleware, and DI configuration. Depends on `GearUp.Infrastructure`.
- **GearUp.UnitTests**: xUnit tests for the application and infrastructure layers.
- **realtime-client**: A Vite/React testing tool for SignalR hubs.

## Key Technologies

- **Backend**: .NET 9, ASP.NET Core, EF Core (Npgsql), Identity, SignalR, RabbitMQ.
- **Database**: PostgreSQL (Persistence), Redis (Caching).
- **Frontend (Test)**: React, Vite, SignalR Client.
- **Observability**: Serilog, OpenTelemetry, Health Checks, Aspire Dashboard.
- **Testing**: xUnit, k6 (Load Testing).

## Building and Running

### Prerequisites

- .NET 9 SDK
- Docker & Docker Compose
- Node.js (for `realtime-client`)

### Backend Commands

- **Build**: `dotnet build`
- **Run API**: `dotnet run --project GearUp.Presentation`
- **Run Tests**: `dotnet test`
- **Update Database**: `dotnet ef database update --project GearUp.Infrastructure --startup-project GearUp.Presentation`
- **Add Migration**: `dotnet ef migrations add <Name> --project GearUp.Infrastructure --startup-project GearUp.Presentation`

### Frontend (realtime-client)

- **Install**: `npm install`
- **Run**: `npm start`

### Docker Compose

- **Start all services**: `docker compose up -d --build` (PostgreSQL, Redis, RabbitMQ, API, Aspire Dashboard).

## Development Conventions

- **Clean Architecture**: Strictly follow the dependency flow (Domain <- Application <- Infrastructure <- Presentation).
- **Repository Pattern**: Use repositories in `Infrastructure` for data access, interfaced in `Application`.
- **Dependency Injection**: Registered in `GearUp.Presentation/Extensions/ServiceExtensions.cs` and `GearUp.Infrastructure/DependencyInjection.cs`.
- **API Versioning**: REST APIs are under `/api/v1/*`.
- **Error Handling**: Handled via `ExceptionMiddleware` in the `Presentation` layer.
- **Real-time**: SignalR hubs are mapped to `/hubs/post`, `/hubs/notification`, and `/hubs/chat`.
- **Messaging**: RabbitMQ is optional and can be disabled via `RabbitMQ:UseRabbitMQ` config. When disabled, an `InMemoryPublisher` is used, and background workers are not registered.
- **Environment Variables**: Managed via `.env` files (loaded at startup in `Program.cs`).

### Naming Conventions

- **Controllers**: `{Entity}Controller.cs` (e.g., `UserController.cs`)
- **Services**: `I{Service}Service.cs` for interfaces, `{Service}Service.cs` for implementations
- **Repositories**: `I{Entity}Repository.cs` for interfaces
- **DTOs**: `{Purpose}{Entity}Dto.cs` (e.g., `RegisterRequestDto.cs`)
- **Validators**: `{Dto}Validator.cs` (e.g., `RegisterRequestDtoValidator.cs`)

### Coding Style

- Use **PascalCase** for class names, methods, and properties.
- Use **camelCase** for local variables and parameters.
- Use **_camelCase** for private fields (with underscore prefix).
- Prefer `async/await` over synchronous operations for I/O.
- Use `DateTime.UtcNow` instead of `DateTime.Now`.

### Commit Guidelines

Follow [Conventional Commits](https://www.conventionalcommits.org/):
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

## Configuration

Crucial environment variables (see `.env.example`):
- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string.
- `Redis__ConnectionString`: Redis connection string.
- `Jwt__AccessToken_SecretKey`: Secret for signing JWTs.
- `APP_STARTUP_MODE`: Controls DB tasks (`web`, `db-migrate`, `db-seed`).

## Documentation

- **API Endpoints**: `GearUp.Presentation/docs/API_ENDPOINTS.md`
- **Contributing**: `CONTRIBUTING.md`
- **Load Testing**: `LoadTests/readme.md`

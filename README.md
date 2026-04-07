# GearUp 🚀

**GearUp** is a modern, modular, and production-ready web application built with **ASP.NET Core**. It provides a robust foundation for scalable systems with authentication, authorization, cloud integrations, Domain Driven Design(DDD) and Clean Architecture.

---

## 🧩 Table of Contents

* [Overview](#overview)
* [Architecture](#architecture)
* [Tech Stack](#tech-stack)
* [Project Structure](#project-structure)
* [Key Features](#key-features)
* [Setup Guide](#setup-guide)
* [Environment Configuration](#environment-configuration)
* [Database and Migrations](#database-and-migrations)
* [Authentication & Authorization](#authentication--authorization)
* [Image & File Uploads](#image--file-uploads)
* [Validation & Error Handling](#validation--error-handling)
* [Best Practices](#best-practices)
* [Development Standards](#development-standards)
* [API Documentation](#api-documentation)
* [Testing](#testing)
* [Deployment](#deployment)
* [Contributing](#contributing)
* [License](#license)

---

## 🧠 Overview

GearUp is designed as a **multi-module backend service** built with clean architecture principles. It emphasizes **separation of concerns**, **testability**, and **scalability**, ideal for SaaS platforms, dashboards, or developer APIs.

---

## 🏗️ Architecture

GearUp follows **Clean Architecture (DDD)** principles:

```
GearUp
├── GearUp.Presentation       → Presentation layer (controllers, filters, middleware)
├── GearUp.Application        → Application logic, CQRS handlers, services
├── GearUp.Domain             → Entities, value objects, enums, domain events
├── GearUp.Infrastructure     → Data access, persistence, external integrations
└── GearUp.Tests              → Unit and integration tests
```

**Core Concepts:**

* **Domain-Driven Design (DDD)**
* **Repository + Unit of Work Pattern**
* **CQRS (Command Query Responsibility Segregation)**
* **FluentValidation** for request validation
* **AutoMapper** for DTO mapping

---

## ⚙️ Tech Stack

| Category             | Technology                            |
| -------------------- | ------------------------------------- |
| **Backend**          | ASP.NET Core 9, C# 12                 |
| **ORM**              | Entity Framework Core                 |
| **Database**         | MySql                                 |
| **Authentication**   | JWT+Refresh Tokens (NextAuth | Social)|
| **Validation**       | FluentValidation                      |
| **Object Mapping**   | AutoMapper                            |
| **Cloud Storage**    | Cloudinary (for images)               |
| **Containerization** | Docker + Docker Compose               |
| **Logging**          | Serilog                               |
| **API Docs**         | Swagger / Swashbuckle                 |
| **Unit Testing**     | XUnit.Net                             |

---

## 🧱 Project Structure

```
GearUp
│
├── GearUp.API
│   ├── Controllers
│   ├── Middlewares
│   ├── Extensions
│   ├── appsettings.json
│   └── Program.cs / Startup.cs
│
├── GearUp.Domain
│   ├── Entities
│   ├── Enums
│   └── ValueObjects
│
├── GearUp.Application
│   ├── Interfaces
│   ├── Services
│   ├── DTOs
│   ├── Handlers
│   └── Validators
│
├── GearUp.Infrastructure
    ├── Data
    ├── Configurations (Fluent API)
    ├── Repositories
    ├── Cloud (Cloudinary, etc.)
    └── DependencyInjection.cs
```

---

## 🌟 Key Features

✅ Modular Domain Architecture
✅ Clean CQRS with MediatR
✅ JWT Authentication & Refresh Tokens
✅ Cloudinary integration for image storage
✅ Role-based, Claim-based, and Policy-based Authorization
✅ FluentValidation for DTOs
✅ Dockerized for container deployment
✅ AutoMapper for data transformation
✅ Comprehensive error handling middleware
✅ Swagger API Documentation
✅ MySql with EF Core migrations
✅ Health checks for database and Redis
✅ API versioning support
✅ Redis caching for improved performance
✅ Rate limiting for API protection
✅ Structured logging with Serilog

---

## ⚙️ Setup Guide

### 1️⃣ Prerequisites

Ensure you have installed:

* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* [Docker](https://www.docker.com/)
* [PostgreSQL](https://www.mysql.com/)
* [Cloudinary Account](https://cloudinary.com/)

### 2️⃣ Clone Repository

```bash
git clone https://github.com/yourusername/GearUp.git
cd GearUp
```

### 3️⃣ Configure Environment

Create a `.env` file or use `appsettings.Development.json` with:

```env
ConnectionStrings__DefaultConnection=your_connection_string
Redis__ConnectionString=localhost:6379
Jwt__Issuer=your_issuer_url
Jwt__Audience=your_audience_url
Jwt__AccessToken_SecretKey=super_strong_secret_key
Jwt__EmailVerificationToken_SecretKey=super_strong_secret_key
FromEmail=your_email
ClientUrl=frontend_url
SendGridApiKey=your_api_key
GOOGLE_CLIENT_ID=your_client_id
ASPNETCORE_ENVIRONMENT=Development
MYSQL_ROOT_PASSWORD=root_password
MYSQL_DATABASE=database_name
CLOUDINARY_URL=cloudinary_url
ADMIN_USERNAME=admin
ADMIN_EMAIL=admin@example.com
ADMIN_PASSWORD=your_secure_admin_password
```

### 4️⃣ Run the Application

**With Docker Compose:**

```bash
docker compose up --build
```

**Without Docker:**

```bash
dotnet ef database update
cd GearUp.API
dotnet run
```

API available at: [http://localhost:5255/swagger](http://localhost:5255/swagger)

---

## 🗄️ Database and Migrations

```bash
# Add migration
dotnet ef migrations add Init --project GearUp.Infrastructure --startup-project GearUp.API

# Apply migration
dotnet ef database update --project GearUp.Infrastructure --startup-project GearUp.API
```

---


## 🧰 DbContext Pooling & Resiliency Startup Notes

`GearUp.Infrastructure/DependencyInjection.cs` configures EF Core MySQL with pooled contexts for throughput and resilience:

* `AddDbContextPool<GearUpDbContext>(poolSize: 128)` to reduce DbContext allocation overhead.
* `EnableRetryOnFailure(...)` for transient MySQL/network fault handling (`maxRetryCount: 5`, `maxRetryDelay: 10s`).
* `CommandTimeout(60)` to allow heavier read/query workloads without premature command cancellation.

### Pooled-context safety assumptions

Because pooled contexts are reused, do **not** store per-request mutable state on `GearUpDbContext` instances. The current `GearUpDbContext` implementation is compatible with pooling because it only exposes `DbSet<>` properties and model configuration, with no request/user-specific fields.

When adding new behavior, keep these rules:

1. Keep request-specific state in scoped services, not on `GearUpDbContext` fields/properties.
2. Avoid changing context-level settings dynamically per request (for example mutable tracking defaults on a shared instance).
3. Continue injecting repositories/services as scoped dependencies so each request gets a leased context instance from the pool.

## 🔐 Authentication & Authorization

GearUp uses **JWT Bearer Tokens** for secure authentication.

### Token Flow:

1. User logs in → receives access + refresh token.
2. Access token expires → refresh token renews session.
3. Claims & Roles stored in JWT payload.

Supported Authorization Types:

* **Role-Based:** via `[Authorize(Roles = "Admin")]`
* **Claim-Based:** via `[Authorize(Policy = "CanViewDashboard")]`

---

## 🖼️ Image & File Uploads

All user images (avatars, KYC, etc.) are stored in **Cloudinary**.

**Folder Structure Example:**

```
gearup/users/{userId}/avatar
gearup/users/{userId}/kyc
```

**Generated URL Example:**

```
https://res.cloudinary.com/<cloud_name>/image/upload/v128724/gearup/users/{userId}/avatar/avatar.jpg
```

---

## ✅ Validation & Error Handling

* **FluentValidation** for DTOs (e.g., `UserRegistrationValidator`)
* **Global Error Middleware** catches and formats exceptions
* Sensitive error details are only exposed in Development environment
* Production errors return generic messages to protect system information

Example Error Response:

```json
{
  "status": 400,
  "message": "The ConfirmedNewPassword field is required.",
  "traceId": "00-6e112a590a..."
}
```

---

## 🏥 Health Checks

GearUp includes built-in health checks to monitor the application and its dependencies:

**Health Check Endpoint:**
```
GET /health
```

**Monitored Components:**
* Database connectivity (MySQL)
* Redis cache availability

Example Health Check Response:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "database": {
      "status": "Healthy"
    },
    "redis": {
      "status": "Healthy"
    }
  }
}
```

---

## 🔄 API Versioning

API versioning is supported through URL versioning:
* Default version: v1.0
* Versions are reported in response headers
* Example: `/api/v1/users`

---

## 🧭 Best Practices

* Keep domain logic **pure** (no EF Core logic in Domain).
* Use **Dependency Injection** for all services.
* Store secrets in **Environment Variables**, not code.
* Use **async/await** for all I/O operations.
* Prefer **DTOs** over entities in API responses.

---

## 💡 Development Standards

| Area               | Standard                                                        |
| ------------------ | --------------------------------------------------------------- |
| Naming             | `PascalCase` for classes, `camelCase` for variables             |
| Folders            | Group by feature/domain, not layer                              |
| Validation         | FluentValidation per DTO                                        |
| Exception Handling | Use custom exceptions + middleware                              |
| Logging            | Structured with Serilog                                         |
| Commits            | Conventional commit format                                      |

---

## 📘 API Documentation

Auto-generated via **Swagger** at:

```
http://localhost:5255/swagger
```

Export OpenAPI specs using:

```bash
dotnet swagger tofile --output swagger.json bin/Debug/net8.0/GearUp.API.dll v1
```

---

## 🧪 Testing

```
dotnet test
```

Includes:

* Unit tests for domain logic
* Integration tests for API endpoints

---

## 🚀 Deployment

### Docker Compose Example

```yaml
services:
  db:
    image: mysql:8.0
    container_name: gearup-db
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD}
      MYSQL_DATABASE: ${MYSQL_DATABASE}
    ports:
      - "3307:3306"
    networks:
      - gearup_network
    volumes:
      - gearup_data:/var/lib/mysql
    healthcheck:
        test: ["CMD", "mysqladmin", "ping", "-h", "localhost"]
        timeout: 20s
        retries: 10

  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5255:8080"
    environment:
      ConnectionStrings__DefaultConnection: ${ConnectionStrings__DefaultConnection}
      Jwt__Issuer: ${Jwt__Issuer}
      Jwt__Audience: ${Jwt__Audience}
      Jwt__AccessToken_SecretKey: ${Jwt__AccessToken_SecretKey}
      Jwt__EmailVerificationToken_SecretKey: ${Jwt__EmailVerificationToken_SecretKey}
      SendGridApiKey: ${SendGridApiKey}
      FromEmail: ${FromEmail}
      ClientUrl: ${ClientUrl}
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT}
      CLOUDINARY_URL: ${CLOUDINARY_URL}
    depends_on:
      - db
    networks:
      - gearup_network
    restart: on-failure

volumes:
  gearup_data:

networks:
    gearup_network:
```

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit changes (`git commit -m 'feat: add new endpoint'`)
4. Push to branch (`git push origin feature/my-feature`)
5. Create a Pull Request

---

## 📄 License

MIT License © 2025 GearUp Team

---

### 🔗 Author & Maintainer

**Shane Htet Aung**
📧 Contact: [shanehtetaung.conceptx.mm@gmail.com](mailto:shanehtetaung.conceptx.mm@gmail.com)
🐙 GitHub: [@Rahull-Adk](https://github.com/Rahull-Adk)

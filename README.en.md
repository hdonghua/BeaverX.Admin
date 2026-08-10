# BeaverX.Admin (Backend)

> **Language**: [简体中文](README.md) | English

ASP.NET Core admin API built on [ABP Framework](https://abp.io/) (`Volo.Abp.*`). Entity primary keys are **Guid**.

Frontend: [beaverx-vue-admin](https://github.com/hdonghua/beaverx-vue-admin)

> **Branches**: `master` (EF Core + PostgreSQL) is the only actively maintained branch. Other `beaverx-xxx` branches are **no longer updated**.

## Implemented Features

| Capability | Description |
|------------|-------------|
| **Realtime messaging** | SignalR + Redis Backplane: unread site messages, export progress, online users, force logout; online presence aggregated by browser device fingerprint, heartbeat + TTL auto offline |
| **Async export** | CAP + Redis Streams: create an export task, generate Excel asynchronously and upload to MinIO; frontend tracks status via SignalR, with idempotent consume and retry |
| **Full workflow** | Process / form design, launch & approve, transfer / add-sign / reduce-sign / rollback / urge / cancel, CC & print, service-task nodes, and other OA approval capabilities |

Also includes RBAC, dictionaries, system config, payment channels, tickets, scheduled jobs (Hangfire), and other common admin modules.

## Tech Stack

| Category | Technology |
|----------|------------|
| Runtime | .NET 10 |
| Web | ASP.NET Core + Volo.Abp.AspNetCore.Mvc |
| ORM | Entity Framework Core + PostgreSQL |
| Primary key | Guid |
| Auth | JWT Bearer + Refresh Token |
| Cache / realtime / messaging | Redis (distributed cache, SignalR Backplane, CAP Redis Streams, online users) |
| Scheduling | Hangfire (PostgreSQL storage) |
| Logging | Serilog (console + local files) |
| Object storage | MinIO (optional) |

**Requirements**: [.NET 10 SDK](https://dotnet.microsoft.com/download), PostgreSQL 14+, Redis 6+; MinIO optional.

## Database Migrations

Configure `ConnectionStrings:Default` in `BeaverX.Admin.Http.Host/appsettings.Development.json`, then before the first run:

```bash
# Switch to the BeaverX.Admin.EntityFrameworkCore directory, open a terminal, and run:

dotnet ef database update
```

Default URL is in `Properties/launchSettings.json` (usually `http://localhost:5216`). Seed data includes admin: **admin / Admin@123**.

## Layer Responsibilities

```
BeaverX.Admin/
├── BeaverX.Admin.Http.Host/             # Entry, appsettings, middleware
├── BeaverX.Admin.Http.Api/              # Controllers, auth filters
├── BeaverX.Admin.Infrastructure/        # JWT, MinIO, CAP, Hangfire, SignalR, etc.
├── BeaverX.Admin.Application/           # AppServices, seeders, orchestration
├── BeaverX.Admin.Application.Contracts/ # DTOs, application / infra interfaces
├── BeaverX.Admin.Domain/                # Entities, domain rules
├── BeaverX.Admin.Domain.Shared/         # Permission codes, enums
└── BeaverX.Admin.EntityFrameworkCore/   # DbContext, migrations
```

| Layer | Responsibility | Examples |
|-------|----------------|----------|
| Domain | Entities, domain rules | `SysConfig`, `Menu` |
| Domain.Shared | Cross-layer constants | `RbacPermissionCodes` |
| Application.Contracts | Contracts | `IConfigAppService`, `IBlobStorage` |
| Application | Business logic | `ConfigAppService`, `AuthAppService` |
| Infrastructure | Technical implementations | `JwtTokenService`, `ExportTaskCapSubscriber` |
| EntityFrameworkCore | Persistence | `AdminDbContext`, migrations |
| Http.Api | HTTP adapters | `ConfigController` |
| Http.Host | Composition root | Module registration, JWT / CORS |

Types implementing `IScopedDependency` / `ITransientDependency` / `ISingletonDependency` are auto-registered by ABP.

## Configuration

Local development settings are centralized in `BeaverX.Admin.Http.Host/appsettings.Development.json`:

| Section | Description |
|---------|-------------|
| `ConnectionStrings:Default` | PostgreSQL connection string |
| `Cache:RedisConnectionString` | Redis (cache / SignalR / CAP / refresh tokens / online users), required |
| `Cache:KeyPrefix` / `DefaultExpirationSeconds` | Cache key prefix and default TTL |
| `Jwt` | Issuer, Audience, SecretKey, expiration |
| `CorsOrgins` | Frontend origins, comma-separated |
| `Minio` | Object storage (optional; needed for export/upload) |
| `Hangfire` | Schema, dashboard, startup sync |
| `Payment` | Payment notify URL, cert paths, etc. |
| `Serilog` | Log levels and `Logs/log-*.txt` |

## License

This project is licensed under the [MIT License](LICENSE).


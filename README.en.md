# BeaverX.Admin (Backend)

> **Language**: [绠€浣撲腑鏂嘳(README.md) | English

ASP.NET Core admin API built on the official [ABP Framework](https://abp.io/) (`Volo.Abp.*` packages)鈥擱BAC, dictionaries, system configuration, messaging, file storage, and more. Entity primary keys are **Guid**.

## Live Demo

| Item | Details |
|------|---------|
| URL | [https://beaverxadmin.com/](https://beaverxadmin.com/) |
| Account | `admin` / `Admin@123` |

> **Demo notice**: Data is reset every **5 minutes**. Do not store important information or use this environment for production.

## Tech Stack

| Category | Technology |
|----------|------------|
| Runtime | .NET 10 |
| Web | ASP.NET Core + Volo.Abp.AspNetCore.Mvc |
| ORM | Entity Framework Core + **PostgreSQL** (`master`) / **MySQL** (`master-mysql`); SqlSugar + **PostgreSQL** (`sqlsugar`) / **MySQL** (`sqlsugar-mysql`) |
| Primary key | **Guid** (ABP `Entity<Guid>` / `FullAuditedEntity<Guid>`, etc.) |
| Auth | JWT Bearer + Refresh Token |
| Cache / realtime / messaging | **Redis** (distributed cache, SignalR backplane, CAP Redis Streams, online users) |
| Logging | Serilog (console + local files) |
| Object storage | MinIO (optional) |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **PostgreSQL 14+** (`master` / `sqlsugar`) or **MySQL 8+** (`master-mysql` / `sqlsugar-mysql`, see below)
- **Redis 6+** (required for cache, SignalR backplane, CAP transport, online users)
- (Optional) MinIO for file uploads
- Frontend: [beaverx-vue-admin](https://github.com/hdonghua/beaverx-vue-admin)

## Database Choice (EF Core / SqlSugar)

The backend uses **Git branches** for ORM / database drivers. **No frontend changes** are required.

| Branch | ORM / Database | Notes |
|--------|----------------|-------|
| `master` (default) | EF Core + PostgreSQL | Main development branch; CAP / Hangfire use PostgreSQL |
| `master-mysql` | EF Core + MySQL 8+ | MySQL variant鈥攕witch branch manually |
| `sqlsugar` | **SqlSugar** + PostgreSQL | CodeFirst auto-syncs tables; **create an empty database first**; change `DbType` for other DBs |
| `sqlsugar-mysql` | **SqlSugar** + MySQL 8+ | SqlSugar MySQL preset; also requires an empty database first |

### Switch to MySQL (`master-mysql`)

```bash
git clone https://github.com/hdonghua/BeaverX.Admin.git
cd BeaverX.Admin

git fetch origin
git checkout master-mysql
```

Edit `BeaverX.Admin.Http.Host/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=3306;Database=beaverx-admin;User=root;Password=your_password;Allow User Variables=True;"
  }
}
```

> `Allow User Variables=True` is required by Hangfire.MySql鈥攄o not omit it.

MySQL branch differences (summary):

- EF Core: `Volo.Abp.EntityFrameworkCore.MySql` (see `master-mysql` branch)
- Hangfire storage: MySQL (table prefix in `Hangfire:SchemaName`)
- CAP message storage: MySQL (same `ConnectionStrings:Default` as the app DB)
- API datetime: global UTC JSON serialization + UTC normalization before save (MySQL `DATETIME` compatibility)

Migration and startup commands are the same as PostgreSQL (see Quick Start). **Do not mix migration histories** for both databases on one branch; to move from PostgreSQL to MySQL, use `master-mysql` and run `dotnet ef database update` again.

The demo site [beaverxadmin.com](https://beaverxadmin.com/) is deployed on the **MySQL** branch.

### Switch to SqlSugar (`sqlsugar`)

```bash
git fetch origin
git checkout sqlsugar
```

Edit `BeaverX.Admin.Http.Host/appsettings.Development.json` (PostgreSQL connection string, same format as `master`):

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=beaverx-admin;Username=postgres;Password=your_password"
  }
}
```

> **Create the database manually first.** The SqlSugar branch does **not** use `dotnet ef database update` to create the database. Hangfire connects on startup and will fail if the database does not exist.
>
> Create an empty PostgreSQL database (e.g. `beaverx-admin`), then start the API. Business tables are **synced automatically** via CodeFirst (`InitTables`); you usually do not need hand-written DDL.

Summary of differences:

- ORM: SqlSugar (`BeaverX.Data.SqlSugar`); no EF Core migrations project
- Database: create an empty database manually
- Tables: auto-synced from entities on startup
- Hangfire / CAP: same business connection string (database must already exist)

```bash
# Create empty DB beaverx-admin first, then:
dotnet run --project BeaverX.Admin.Http.Host
```

### Switch to SqlSugar + MySQL (`sqlsugar-mysql`)

```bash
git fetch origin
git checkout sqlsugar-mysql
```

Edit the connection string (similar to `master-mysql`):

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=3306;Database=beaverx-admin;User=root;Password=your_password;Allow User Variables=True;"
  }
}
```

> Also **create an empty database first**; tables sync on startup. `Allow User Variables=True` is required by Hangfire.MySql.

### Switch to other databases (SQL Server / Oracle, etc.)

Official branches only preset **PostgreSQL** and **MySQL**. For **SQL Server**, **Oracle**, and others:

#### EF Core (`master` / `master-mysql`)

This repo does **not** preset SQL Server / Oracle drivers (`master` uses `Volo.Abp.EntityFrameworkCore.PostgreSql`).

You must implement them yourself (follow the existing PostgreSQL / MySQL branches and `AbpDbContextOptions`):

1. **EF Core driver**: configure `UseSqlServer` / `UseOracle` in `BeaverXAdminEntityFrameworkCoreModule` and add the matching ABP / EF packages
2. **Admin**: wire DbContext, repositories, migrations; also adapt Hangfire / CAP storage for the target database
3. Recreate and apply **EF Migrations** (do not mix migration histories across databases)

Application/domain entities can be reused; drivers and infrastructure must be adapted by you.

#### SqlSugar (`sqlsugar` / `sqlsugar-mysql`)

Change `BeaverXSqlSugarOptions.DbType` (or `AddBeaverXSqlSugar(..., DbType.Xxx, ...)`) to the target database, for example:

```csharp
options.DbType = DbType.SqlServer; // or DbType.Oracle, DbType.MySql, etc.
options.ConnectionString = "your connection string";
```

Also update the connection string and **create an empty database first**; tables still sync via CodeFirst. Adapt Hangfire / CAP storage if no built-in provider exists for that database.

## Quick Start

### 1. Configure Database and Redis

Edit `BeaverX.Admin.Http.Host/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=beaverx-admin;Username=postgres;Password=postgres;..."
  },
  "Cache": {
    "RedisConnectionString": "localhost:6379"
  }
}
```

Configure Redis only via `Cache:RedisConnectionString` in `appsettings.json`.

> On `sqlsugar` / `sqlsugar-mysql`: **create the empty database first**, then start the API (tables sync automatically).

### 2. Run Migrations

`master` / `master-mysql` (EF Core):

```bash
cd BeaverX.Admin

dotnet ef database update \
  --project BeaverX.Admin.EntityFrameworkCore \
  --startup-project BeaverX.Admin.Http.Host
```

`sqlsugar` / `sqlsugar-mysql`: **skip this step** (no EF migrations); ensure the empty database already exists.

### 3. Start API

```bash
dotnet run --project BeaverX.Admin.Http.Host
```

Default URL: `http://localhost:5216` (see `Properties/launchSettings.json`)

### 4. Seed Data

On startup, ABP `IDataSeeder` runs all `IDataSeedContributor` implementations, including:

- RBAC (users, roles, menus, super admin `super_admin`)
- Dictionary, config, message demo data
- Module menus and button permissions

Default admin: **admin / Admin@123**

## Solution Structure

```
BeaverX.Admin/
├── BeaverX.Admin.Http.Host/             # Entry point, appsettings, Serilog, JWT/CORS
├── BeaverX.Admin.Http.Api/              # Controllers, auth filters
├── BeaverX.Admin.Infrastructure/        # MinIO, CAP, JWT signing, password hashing
├── BeaverX.Admin.Application/           # AppServices, seeders, orchestration
├── BeaverX.Admin.Application.Contracts/ # DTOs, IAppService, infrastructure interfaces
├── BeaverX.Admin.Domain/                # Entities
├── BeaverX.Admin.Domain.Shared/         # Permission codes, enums
└── BeaverX.Admin.EntityFrameworkCore/   # DbContext, migrations
```

### Layer Responsibilities

| Layer | Role | Examples |
|-------|------|----------|
| Domain | Entities, domain rules | `SysConfig`, `Menu` |
| Domain.Shared | Cross-layer constants | `RbacPermissionCodes` |
| Application.Contracts | Public contracts | `IConfigAppService`, `IBlobStorage`, `IJwtTokenService` |
| Application | Business logic | `ConfigAppService`, `ExportTaskMessageService` |
| Infrastructure | Technical implementations | `MinioBlobStorage`, `JwtTokenService`, `ExportTaskCapSubscriber` |
| EntityFrameworkCore | Persistence | `AdminDbContext`, migrations |
| Http.Api | HTTP adapters | `ConfigController` |
| Http.Host | Composition root, middleware | JWT Bearer, CORS, module registration |

### Dependency Injection

Classes implementing `IScopedDependency` (or `ITransientDependency` / `ISingletonDependency`) are auto-registered by **ABP**. AppServices implement their interface and are injected into controllers.

## API Conventions

- Route prefix: `/api/[Controller]` (inherit `AdminControllerBase` 鈫?`AbpControllerBase`)
- Entity / DTO primary keys: `Guid` (route constraint `{id:guid}`)
- Permissions: `[RequirePermission("system:xxx:yyy")]` on controller actions
- Permission codes: `BeaverX.Admin.Domain.Shared/Rbac/RbacPermissionCodes.cs`
- Business errors: throw `BusinessException` (`Domain.Shared`); `BusinessExceptionFilter` returns JSON

## Configuration

| Section | File | Description |
|---------|------|-------------|
| `ConnectionStrings:Default` | appsettings.Development.json | PostgreSQL (`master`) or MySQL (`master-mysql`) |
| `Cache:RedisConnectionString` | appsettings.json | Redis (cache / SignalR / CAP / online users) |
| `Jwt` | appsettings.json | Issue and validate tokens |
| `CorsOrgins` | appsettings.Development.json | Frontend origins (comma-separated) |
| `Minio` | appsettings.json | File storage (optional) |
| `Cache` | appsettings.json | Redis key prefix, connection string, default TTL |
| `Serilog` | appsettings.json | Log levels; files at `Logs/log-*.txt` |

## Database Migrations

```bash
# Add migration
dotnet ef migrations add <MigrationName> \
  --project BeaverX.Admin.EntityFrameworkCore \
  --startup-project BeaverX.Admin.Http.Host

# Update database
dotnet ef database update \
  --project BeaverX.Admin.EntityFrameworkCore \
  --startup-project BeaverX.Admin.Http.Host

# Roll back to a migration
dotnet ef database update <PreviousMigrationName> \
  --project BeaverX.Admin.EntityFrameworkCore \
  --startup-project BeaverX.Admin.Http.Host
```

After adding entities, configure table names, indexes, and column lengths in `AdminDbContext.OnModelCreating`.

## Adding a Business Module (Standard Flow)

Example: **System Configuration**.

### 1. Domain Entity

`BeaverX.Admin.Domain/Config/SysConfig.cs`, inherit `FullAuditedEntity<Guid>`.

### 2. DbContext

Add `DbSet<SysConfig>` and `OnModelCreating` config in `AdminDbContext`, then migrate.

### 3. Permission Codes

In `RbacPermissionCodes.cs`:

```csharp
public static class Config
{
    public const string List = "system:config:list";
    public const string Create = "system:config:create";
    // ...
}
```

### 4. Contracts

- `Application.Contracts/Config/Dtos/ConfigDtos.cs`
- `Application.Contracts/Config/IConfigAppService.cs`

### 5. Application Service

`Application/Config/ConfigAppService.cs`:

- Implement `IConfigAppService` + `IScopedDependency`
- Use `IRepository<T>` for data access
- Throw `BusinessException` on validation failure

### 6. Controller

`Http.Api/Controllers/ConfigController.cs`:

```csharp
public class ConfigController : AdminControllerBase
{
    [RequirePermission(RbacPermissionCodes.System.Config.List)]
    [HttpGet("list")]
    public Task<PagedResultDto<ConfigDto>> GetListAsync(...) => ...;
}
```

### 7. Menu & Seed

- `ConfigMenuSeeder`: insert menu, `path`, `component`, button permissions; assign to `super_admin`
- `ConfigDataSeeder` (optional): demo data
- Implement `IDataSeedContributor` + `ITransientDependency`; ABP `IDataSeeder` runs them on startup

Menu fields must align with the frontend:

| Field | Example | Description |
|-------|---------|-------------|
| `Path` | `/system/config` | Route URL (**customizable**) |
| `Component` | `system/config/index` | Must match `views/system/config/index.vue` |
| `Perms` | `system:config:list` | Page access permission |

The frontend matches pages by `Component` and registers/displays routes by `Path`.

### 8. Frontend Integration

See [beaverx-vue-admin README](https://github.com/hdonghua/beaverx-vue-admin):

1. Add static route in `router/routes/modules/` and page under `views/`
2. Configure the same `Component` in menu management (or seed); `Path` as needed
3. Add permission constants in `constants/permissions.ts` matching `RbacPermissionCodes` (for `v-permission`)
4. Assign menus to roles and test

**Mismatched `component`** is the most common cause of 403 (e.g. backend `system/configs/index` vs frontend `system/config/index`).

## RBAC Notes

- Super admin role code: `super_admin`鈥攆ull menu access (auto full set on query/assign)
- Menu types: directory / menu / button; buttons use `IsVisible = false` for API permissions
- Hidden menus: `IsVisible = false`鈥攏ot in sidebar, but routable when authorized

## Site Messages (Admin Send)

| API | Permission | Description |
|-----|------------|-------------|
| `POST /api/SiteMessageAdmin/send` | `system:message:send` | Send to specific users or all enabled users |

Sending uses `IMessageSender` 鈫?`site` channel (`SiteMessageChannelSender`), writes `user_messages`, and pushes `message.unread.changed` via SignalR.

Frontend page: `/system/message` (send site message); menu seeded by `MessageMenuSeeder` on startup.

## Realtime Notifications (SignalR)

Export tasks and unread messages are pushed via SignalR instead of polling. **Redis Backplane** and **`RedisOnlineUserTracker`** are enabled by default for multi-instance push.

| Component | Description |
|-----------|-------------|
| `IRealtimeNotifier` | Generic push interface (Contracts) |
| `SignalRRealtimeNotifier` | SignalR implementation (Infrastructure) |
| `RealtimePublisher` | Builds payload and pushes |
| `AdminNotificationHub` | Hub at `/hubs/notifications` |
| Redis Backplane | `AddStackExchangeRedis`, channel prefix `BeaverXAdmin:SignalR:` |

### Events

| Event | When | Payload |
|-------|------|---------|
| `export.task.changed` | Create/claim/complete/fail | `{ task, activeCount }` |
| `message.unread.changed` | Mark read, etc. | `{ unreadCount }` |

Clients connect with JWT (`accessTokenFactory` or query `access_token`); server targets users by `ClaimTypes.NameIdentifier`.

## Message Sending (Multi-Channel)

Read/mark-read APIs: `IMessageAppService`. **Sending** goes through `IMessageSender` for future channels (DingTalk, WeCom, etc.).

| Component | Description |
|-----------|-------------|
| `IMessageSender` | Send facade (Contracts) |
| `IMessageChannelSender` | Single-channel sender |
| `MessageSender` | Dispatches by channel (Application) |
| `MessageChannelRegistry` | Channel registry |
| `SiteMessageChannelSender` | Site message: `user_messages` + unread push |

### Channel Constants

`MessageChannels`: `site`, `dingtalk`, `wecom` (reserved).

### Usage

Inject `IMessageSender`:

```csharp
await _messageSender.SendAsync(new SendMessageRequest
{
    UserId = userId,
    Type = UserMessageTypes.Notice,
    Title = "Export complete",
    Content = "Your export is ready. Please download.",
    Channels = [MessageChannels.Site]  // omit to send to all registered channels
}, cancellationToken);
```

### Adding a Channel

1. Implement `IMessageChannelSender` in `Infrastructure` (or a package) with a `Channel` constant
2. Implement `IScopedDependency` for auto DI registration
3. Callers pass `Channels` or broadcast to all registered channels by default

## Scheduled Jobs (Hangfire)

**Hangfire + PostgreSQL / MySQL** (by branch: `master` / `master-mysql`), two coexisting recurring job styles:

| Style | Description | Hangfire Job Id |
|-------|-------------|-----------------|
| **HTTP API jobs** | Admin UI 鈥淪ystem 鈫?Scheduled Jobs鈥?or `POST /api/ScheduledJob`鈥擧TTP URL on cron | `scheduled-job:{id}` |
| **Code `IRecurringJob`** | Implement interface + DI; synced to Hangfire on startup | Type full name |

### Style 1: HTTP API Jobs

- Tables: `sys_scheduled_jobs`, `sys_scheduled_job_logs`
- Frontend: `/system/job` (permissions `system:job:*`)
- Create/update syncs via `IHangfireScheduledJobRegistrar`; manual trigger, cron validation, execution logs
- Current `JobType` supports **HttpApi** only (GET/POST/PUT/DELETE)

```http
POST /api/ScheduledJob
{
  "jobCode": "health-check",
  "name": "Health check",
  "jobType": 1,
  "cronExpression": "0 */5 * * *",
  "httpMethod": 1,
  "httpUrl": "http://localhost:5216/api/Health",
  "timeoutSeconds": 30
}
```

### Style 2: Code `IRecurringJob`

Implement `IRecurringJob` (`CronExpression` + `ExecuteAsync`), inherit `IScopedDependency`; `CodeRecurringJobSyncHostedService` registers on startup.

```csharp
public class SampleDailyRecurringJob : IRecurringJob
{
    public string CronExpression => "0 0 * * *";

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Inject IRepository / IAppService
        return Task.CompletedTask;
    }
}
```

Reference: `Application/Scheduling/Jobs/SampleDailyRecurringJob.cs`

### Config & Dashboard

```json
{
  "Hangfire": {
    "SchemaName": "hangfire",
    "EnableDashboard": true,
    "DashboardPath": "/hangfire",
    "SyncBusinessJobsOnStartup": true,
    "BusinessJobStartupSyncMode": "MergeFromHangfire",
    "Auth": { "Enabled": true, "Username": "hangfire", "Password": "hangfire123" }
  }
}
```

- Dashboard: `/hangfire` (HTTP Basic鈥攏ot business JWT)
- Multi-instance: Hangfire uses DB persistence (PostgreSQL or MySQL); multiple workers OK鈥?*jobs must be idempotent**

Detailed guide: `doc-beaverx-admin/docs/backend/scheduled-jobs.md`.

## Async Export (DotNetCap)

Export uses **CAP + DB storage (PostgreSQL / MySQL by branch) + Redis Streams transport + MinIO files**:

| Component | Description |
|-----------|-------------|
| `export_tasks` | Task table (status, params, file link) |
| `local_message_outbox` | CAP dedup by `cap_message_id` (process each message once) |
| `cap` schema | CAP published/received tables |
| Redis Streams | CAP transport (`DotNetCore.CAP.RedisStreams`) |
| `ExportTaskCapSubscriber` (Infrastructure) | Consumer: Excel in memory → MinIO |

### Flow

1. `POST /api/ExportTask` creates `export_tasks`, publishes CAP message
2. `ICapPublisher` publishes `export.task.execute` (via Redis Streams)
3. Consumer checks `cap_message_id` not consumed → claim (`Pending → Processing`) → export → MinIO → `Completed` → record `cap_message_id`
4. `ExportTaskRecoveryHostedService` requeues stuck Pending tasks on startup

### Idempotency

- **CAP layer** (`CapMessageConsumeService`): after success, write `local_message_outbox.cap_message_id`; replays skip
- **Business layer** (e.g. `ExportTaskMessageService`): atomic claim on `export_tasks.Status` (`Pending → Processing`)
- **Retry**: on failure, status back to `Pending`; CAP retries (max 5); `cap_message_id` only after success

New CAP consumers: ensure business idempotency; call `CapMessageConsumeService.MarkConsumedAsync(capMessageId)` after success.

### Extending Export Types

Implement `IExportHandler`, register `ExportType` constant; frontend passes `exportType` and `parameters`.

## Caching (Redis)

Generic cache via `ICacheService` (Contracts) + `CacheService` (Infrastructure). **Redis distributed cache is required** (no Memory driver).

### Configuration

```json
{
  "Cache": {
    "KeyPrefix": "beaverx:admin:",
    "RedisConnectionString": "localhost:6379",
    "DefaultExpirationSeconds": 3600
  }
}
```

| Field | Description |
|-------|-------------|
| `KeyPrefix` | Global key prefix, e.g. `beaverx:admin:` |
| `RedisConnectionString` | Redis connection string (required) |
| `DefaultExpirationSeconds` | Default TTL when `SetAsync` omits expiration |

### Usage

Inject `ICacheService` in AppService:

```csharp
var user = await _cache.GetOrSetAsync(
    $"user:{id}",
    ct => LoadUserFromDbAsync(id, ct),
    TimeSpan.FromMinutes(10),
    cancellationToken);
```

Use logical keys (e.g. `user:1`); prefix is applied from config.

## Multi-Node Deployment

Cache, SignalR, CAP, and online users **already use Redis by default** (see `BeaverXAdminInfrastructureModule`). For multi-instance deploys, share the same Redis and database across nodes.

| Capability | Implementation |
|------------|----------------|
| Cache `ICacheService` | `AddStackExchangeRedisCache` |
| SignalR | Redis Backplane (`AddStackExchangeRedis`) |
| Online users `IOnlineUserTracker` | `RedisOnlineUserTracker` (Redis Hash) |
| CAP export | `UseRedis` (Redis Streams) |
| Hangfire | DB persistence (PostgreSQL / MySQL); ensure idempotent jobs with multiple workers |
| JWT / DB / MinIO | No node affinity |

Connection string is read only from `Cache:RedisConnectionString` (`RedisConnectionHelper`).

### Checklist

- [ ] Database (PostgreSQL or MySQL), Redis, MinIO reachable from all API instances
- [ ] Same Redis / JWT / CORS config on all instances
- [ ] Load balancer WebSocket sticky sessions **or** Backplane (Backplane is built-in; sticky not required)

## Logging

- Console + `BeaverX.Admin.Http.Host/Logs/log-YYYYMMDD.txt`
- Override `Serilog:MinimumLevel` in `appsettings.Development.json` for dev
- HTTP request logging: `UseSerilogRequestLogging()`

## FAQ

| Symptom | Check |
|---------|-------|
| Migration fails | Connection string; `--startup-project` specified |
| No seed data after start | `IDataSeedContributor` implemented; existing data (seeders skip idempotently) |
| Frontend 403 | Role has menus; `Component` matches `views/`; permission codes match controller |
| CORS errors | `CorsOrgins` includes frontend URL |
| MinIO errors | Export/upload needs MinIO鈥攙erify service and config |
| Export stuck Pending | CAP / Redis running; check `cap` schema and `Logs/` |
| Startup missing Redis connection | Set `Cache:RedisConnectionString` |

## Related Repositories

- Admin frontend: [beaverx-vue-admin](https://github.com/hdonghua/beaverx-vue-admin)

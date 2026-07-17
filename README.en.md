# BeaverX.Admin (Backend)

> **Language**: [简体中文](README.md) | English

ASP.NET Core admin API built on the modular [BeaverX](https://www.nuget.org/packages/BeaverX.Core) framework—RBAC, dictionaries, system configuration, messaging, file storage, and more.

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
| Web | ASP.NET Core + BeaverX.WebMvc |
| ORM | Entity Framework Core + **PostgreSQL** (`master`) / **MySQL** (`master-mysql`); SqlSugar + **PostgreSQL** (`sqlsugar`) / **MySQL** (`sqlsugar-mysql`) |
| Auth | JWT Bearer + Refresh Token |
| Logging | Serilog (console + local files) |
| Object storage | MinIO (optional) |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **PostgreSQL 14+** (`master` / `sqlsugar`) or **MySQL 8+** (`master-mysql` / `sqlsugar-mysql`—see below)
- (Optional) MinIO for file uploads
- Frontend: [beaverx-vue-admin](https://github.com/hdonghua/beaverx-vue-admin)

## Database Choice (EF Core / SqlSugar)

The backend uses **Git branches** for ORM / database drivers. **No frontend changes** are required.

| Branch | ORM / Database | Notes |
|--------|----------------|-------|
| `master` (default) | EF Core + PostgreSQL | Main development branch; CAP / Hangfire use PostgreSQL |
| `master-mysql` | EF Core + MySQL 8+ | MySQL variant—switch branch manually |
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

> `Allow User Variables=True` is required by Hangfire.MySql—do not omit it.

MySQL branch differences (summary):

- EF Core: `BeaverX.EntityFrameworkCore.MySql` + `AdminMySqlDbDriverOptionsBuilder`
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

BeaverX does **not** ship official SQL Server / Oracle driver packages (only `BeaverX.EntityFrameworkCore.PostgreSql` / `BeaverX.EntityFrameworkCore.MySql`).

You must implement them yourself (follow the existing PostgreSQL / MySQL drivers and Admin `IDbDriverOptionsBuilder`):

1. **BeaverX.EntityFrameworkCore.\***: implement `IDbDriverOptionsBuilder` (`UseSqlServer` / `UseOracle`, etc.) and register the module
2. **BeaverX.Domain / Admin**: wire DbContext, repositories, migrations; also adapt Hangfire / CAP storage for the target database
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

### 1. Configure Database

Edit `BeaverX.Admin.Http.Host/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=beaverx-admin;Username=postgres;Password=postgres;..."
  }
}
```

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

On startup, `DataSeederHostService` runs all `IDataSeeder` implementations, including:

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
├── BeaverX.Admin.Domain/                # Entities, IDataSeeder
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

Classes implementing `IScopedDependency` (or `ITransientDependency` / `ISingletonDependency`) are auto-registered by BeaverX. AppServices implement their interface and are injected into controllers.

## API Conventions

- Route prefix: `/api/[Controller]` (inherit `BeaverXController`)
- Permissions: `[RequirePermission("system:xxx:yyy")]` on controller actions
- Permission codes: `BeaverX.Admin.Domain.Shared/Rbac/RbacPermissionCodes.cs`
- Business errors: throw `BusinessException` (`Domain.Shared`); `BusinessExceptionFilter` returns JSON

## Configuration

| Section | File | Description |
|---------|------|-------------|
| `ConnectionStrings:Default` | appsettings.Development.json | PostgreSQL (`master`) or MySQL (`master-mysql`) |
| `Jwt` | appsettings.json | Issue and validate tokens |
| `CorsOrgins` | appsettings.Development.json | Frontend origins (comma-separated) |
| `Minio` | appsettings.json | File storage (optional) |
| `Cache` | appsettings.json | Cache driver (Memory/Redis), key prefix, default TTL |
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

`BeaverX.Admin.Domain/Config/SysConfig.cs`, inherit `FullAuditedEntity`.

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
public class ConfigController : BeaverXController
{
    [RequirePermission(RbacPermissionCodes.System.Config.List)]
    [HttpGet("list")]
    public Task<PagedResultDto<ConfigDto>> GetListAsync(...) => ...;
}
```

### 7. Menu & Seed

- `ConfigMenuSeeder`: insert menu, `path`, `component`, button permissions; assign to `super_admin`
- `ConfigDataSeeder` (optional): demo data
- Implement `IDataSeeder` + `IScopedDependency` for auto-run by `DataSeederHostService`

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

- Super admin role code: `super_admin`—full menu access (auto full set on query/assign)
- Menu types: directory / menu / button; buttons use `IsVisible = false` for API permissions
- Hidden menus: `IsVisible = false`—not in sidebar, but routable when authorized

## Site Messages (Admin Send)

| API | Permission | Description |
|-----|------------|-------------|
| `POST /api/SiteMessageAdmin/send` | `system:message:send` | Send to specific users or all enabled users |

Sending uses `IMessageSender` → `site` channel (`SiteMessageChannelSender`), writes `user_messages`, and pushes `message.unread.changed` via SignalR.

Frontend page: `/system/message` (send site message); menu seeded by `MessageMenuSeeder` on startup.

## Realtime Notifications (SignalR)

Export tasks and unread messages are pushed via SignalR instead of polling.

| Component | Description |
|-----------|-------------|
| `IRealtimeNotifier` | Generic push interface (Contracts) |
| `SignalRRealtimeNotifier` | SignalR implementation (Infrastructure) |
| `RealtimePublisher` | Builds payload and pushes |
| `AdminNotificationHub` | Hub at `/hubs/notifications` |

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
| **HTTP API jobs** | Admin UI “System → Scheduled Jobs” or `POST /api/ScheduledJob`—HTTP URL on cron | `scheduled-job:{id}` |
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

- Dashboard: `/hangfire` (HTTP Basic—not business JWT)
- Multi-instance: Hangfire uses DB persistence (PostgreSQL or MySQL); multiple workers OK—**jobs must be idempotent**

Detailed guide: `doc-beaverx-admin/docs/backend/scheduled-jobs.md`.

## Async Export (DotNetCap)

Export uses **CAP + DB storage (PostgreSQL / MySQL by branch) + in-memory queue + MinIO files**:

| Component | Description |
|-----------|-------------|
| `export_tasks` | Task table (status, params, file link) |
| `local_message_outbox` | CAP dedup by `cap_message_id` (process each message once) |
| `cap` schema | CAP published/received tables |
| `ExportTaskCapSubscriber` (Infrastructure) | Consumer: Excel in memory → MinIO |

### Flow

1. `POST /api/ExportTask` creates `export_tasks`, publishes CAP message
2. `ICapPublisher` publishes `export.task.execute`
3. Consumer checks `cap_message_id` not consumed → claim (`Pending → Processing`) → export → MinIO → `Completed` → record `cap_message_id`
4. `ExportTaskRecoveryHostedService` requeues stuck Pending tasks on startup

### Idempotency

- **CAP layer** (`CapMessageConsumeService`): after success, write `local_message_outbox.cap_message_id`; replays skip
- **Business layer** (e.g. `ExportTaskMessageService`): atomic claim on `export_tasks.Status` (`Pending → Processing`)
- **Retry**: on failure, status back to `Pending`; CAP retries (max 5); `cap_message_id` only after success

New CAP consumers: ensure business idempotency; call `CapMessageConsumeService.MarkConsumedAsync(capMessageId)` after success.

### Extending Export Types

Implement `IExportHandler`, register `ExportType` constant; frontend passes `exportType` and `parameters`.

### Production

Default: `Savorboard.CAP.InMemoryMessageQueue` (single instance). For multi-instance, use Redis / RabbitMQ transport.

## Caching

Generic cache via `ICacheService` (Contracts) + `CacheService` (Infrastructure)—**Memory** / **Redis** drivers.

### Configuration

```json
{
  "Cache": {
    "Driver": "Memory",
    "KeyPrefix": "beaverx:admin:",
    "RedisConnectionString": "localhost:6379",
    "DefaultExpirationSeconds": 3600
  }
}
```

| Field | Description |
|-------|-------------|
| `Driver` | `Memory` (default, single instance) or `Redis` (shared) |
| `KeyPrefix` | Global key prefix, e.g. `beaverx:admin:` |
| `RedisConnectionString` | Redis connection; falls back to `ConnectionStrings:Redis` |
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

Default config targets **single-instance** dev/small deployments. For horizontal scale (multiple pods/processes), switch to shared storage or distributed middleware:

| Capability | Single instance (default) | Multi-node adjustment |
|------------|---------------------------|------------------------|
| Cache `ICacheService` | `Cache:Driver = Memory` | **Redis** + `RedisConnectionString` |
| SignalR | Local hub connections | **Redis Backplane**—otherwise `SendToUser` only hits local connections |
| Online users `IOnlineUserTracker` | In-memory `OnlineUserTracker` | **`RedisOnlineUserTracker`** (StackExchange.Redis `IDatabase`) |
| CAP export | `UseInMemoryMessageQueue()` | Redis / RabbitMQ shared queue |
| Hangfire | DB persistence (PostgreSQL / MySQL) | Multiple workers OK—ensure idempotent jobs |
| JWT / DB / MinIO | No node affinity | Usually unchanged |

### 1. Redis Cache

```json
{
  "Cache": {
    "Driver": "Redis",
    "KeyPrefix": "beaverx:admin:",
    "RedisConnectionString": "localhost:6379"
  }
}
```

### 2. SignalR Redis Backplane

In `BeaverXAdminInfrastructureModule`, add Backplane to `AddSignalR()` (package `Microsoft.AspNetCore.SignalR.StackExchangeRedis`):

```csharp
using BeaverX.Admin.Infrastructure.Realtime;

var redisConnection = RealtimeDistributedExtensions.ResolveRedisConnectionString(configuration);

services.AddSignalR()
    .AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("BeaverXAdmin:SignalR:");
    })
    .AddJsonProtocol(/* keep existing JSON config */);
```

Frontend still connects to `/hubs/notifications` behind the load balancer; Backplane forwards across nodes.

### 3. Online Users: RedisOnlineUserTracker

Implementation: `Infrastructure/Realtime/RedisOnlineUserTracker.cs`—uses injected **`IDatabase`** for Redis Hash (key: `{KeyPrefix}online:connections`), **not** `IDistributedCache`.

**Disabled by default**. In `BeaverXAdminHttpHostModule.ConfigureServices`, **after** Infrastructure module:

```csharp
using BeaverX.Admin.Infrastructure.Realtime;

public override void ConfigureServices(ServiceConfigurationContext context)
{
    // ... existing JWT, CORS, etc. ...

    context.Services.AddRedisOnlineUserTracker(context.Configuration);
}
```

`AddRedisOnlineUserTracker`:

1. Registers `IConnectionMultiplexer` and `IDatabase` (from `Cache:RedisConnectionString` or `ConnectionStrings:Redis`)
2. **Replaces** default `OnlineUserTracker` with `RedisOnlineUserTracker`

Manual registration if needed:

```csharp
services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(configuration["Cache:RedisConnectionString"]!));
services.AddSingleton(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
services.Replace(ServiceDescriptor.Singleton<IOnlineUserTracker, RedisOnlineUserTracker>());
```

### 4. CAP Message Queue

In `BeaverXAdminInfrastructureModule.ConfigureCap`, replace `UseInMemoryMessageQueue()` with e.g.:

```csharp
// Reference DotNetCore.CAP.RedisStreams or DotNetCore.CAP.RabbitMQ
options.UseRedis(redisConnectionString);
```

Otherwise export messages stay in-process and other nodes cannot consume them.

### 5. Checklist

- [ ] Database (PostgreSQL or MySQL), Redis, MinIO reachable from all API instances
- [ ] `Cache:Driver = Redis`
- [ ] SignalR Redis Backplane configured
- [ ] Host module calls `AddRedisOnlineUserTracker`
- [ ] CAP uses shared queue
- [ ] Load balancer WebSocket sticky sessions **or** Backplane (Backplane preferred; sticky not required)
- [ ] Same `Jwt:SecretKey` and CORS on all instances

## Logging

- Console + `BeaverX.Admin.Http.Host/Logs/log-YYYYMMDD.txt`
- Override `Serilog:MinimumLevel` in `appsettings.Development.json` for dev
- HTTP request logging: `UseSerilogRequestLogging()`

## FAQ

| Symptom | Check |
|---------|-------|
| Migration fails | Connection string; `--startup-project` specified |
| No seed data after start | `IDataSeeder` implemented; existing data (seeders skip idempotently) |
| Frontend 403 | Role has menus; `Component` matches `views/`; permission codes match controller |
| CORS errors | `CorsOrgins` includes frontend URL |
| MinIO errors | Export/upload needs MinIO—verify service and config |
| Export stuck Pending | CAP running; check `cap` schema and `Logs/` |

## Related Repositories

- Admin frontend: [beaverx-vue-admin](https://github.com/hdonghua/beaverx-vue-admin)

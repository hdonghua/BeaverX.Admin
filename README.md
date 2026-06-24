# BeaverX.Admin（后端）

> **Language**: 简体中文 | [English](README.en.md)

基于 [BeaverX](https://www.nuget.org/packages/BeaverX.Core) 模块化框架的 ASP.NET Core 管理后台 API，提供 RBAC、字典、系统配置、消息、文件存储等能力。

## 在线预览

| 项目 | 说明 |
|------|------|
| 地址 | [https://beaverxadmin.com/](https://beaverxadmin.com/) |
| 账号 | `admin` / `Admin@123` |

> **演示环境说明**：系统每 **5 分钟** 会定时清理并覆盖数据，请勿保存重要信息或用于生产。

## 技术栈

| 类别 | 技术 |
|------|------|
| 运行时 | .NET 10 |
| Web | ASP.NET Core + BeaverX.WebMvc |
| ORM | Entity Framework Core + **PostgreSQL**（默认分支）/ **MySQL**（`master-mysql` 分支） |
| 认证 | JWT Bearer + Refresh Token |
| 日志 | Serilog（控制台 + 本地文件） |
| 对象存储 | MinIO（可选） |

## 环境要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **PostgreSQL 14+**（`master` 默认分支）或 **MySQL 8+**（`master-mysql` 分支，见下文）
- （可选）MinIO，用于文件上传
- 前端项目：[beaverx-vue-admin](https://github.com/hdonghua/beaverx-vue-admin)

## 数据库选型（PostgreSQL / MySQL）

后端按 **Git 分支** 区分数据库驱动，**前端无需改动**。

| 分支 | 数据库 | 说明 |
|------|--------|------|
| `master`（默认） | PostgreSQL | 主开发分支，CAP / Hangfire 使用 PostgreSQL |
| `master-mysql` | MySQL 8+ | MySQL 版本，需手动切换分支 |

### 切换到 MySQL（`master-mysql`）

```bash
git clone https://github.com/hdonghua/BeaverX.Admin.git
cd BeaverX.Admin

git fetch origin
git checkout master-mysql
```

编辑 `BeaverX.Admin.Http.Host/appsettings.Development.json`：

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=3306;Database=beaverx-admin;User=root;Password=你的密码;Allow User Variables=True;"
  }
}
```

> `Allow User Variables=True` 为 Hangfire.MySql 所需，请勿省略。

MySQL 分支差异摘要：

- EF Core 驱动：`BeaverX.EntityFrameworkCore.MySql` + `AdminMySqlDbDriverOptionsBuilder`
- Hangfire 存储：MySQL（表前缀见 `Hangfire:SchemaName`）
- CAP 消息存储：MySQL（与业务库共用 `ConnectionStrings:Default`）
- API 时间字段：全局 UTC JSON 序列化 + 写入库前 UTC 规范化（兼容 MySQL `DATETIME`）

迁移与启动命令与 PostgreSQL 相同（见「快速开始」）。**不要在同一分支混用两种数据库的迁移历史**；从 PostgreSQL 迁到 MySQL 请使用 `master-mysql` 分支重新 `dotnet ef database update`。

演示站点 [beaverxadmin.com](https://beaverxadmin.com/) 使用 **MySQL** 分支部署。

## 快速开始

### 1. 配置数据库

编辑 `BeaverX.Admin.Http.Host/appsettings.Development.json`：

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=beaverx-admin;Username=postgres;Password=postgres;..."
  }
}
```

### 2. 执行迁移

```bash
cd BeaverX.Admin

dotnet ef database update \
  --project BeaverX.Admin.EntityFrameworkCore \
  --startup-project BeaverX.Admin.Http.Host
```

### 3. 启动 API

```bash
dotnet run --project BeaverX.Admin.Http.Host
```

默认地址：`http://localhost:5216`（见 `Properties/launchSettings.json`）

### 4. 种子数据

应用启动时 `DataSeederHostService` 会自动执行所有 `IDataSeeder` 实现，包括：

- RBAC（用户、角色、菜单、超级管理员 `super_admin`）
- 字典、配置、消息等演示数据
- 各模块菜单与按钮权限

默认管理员：**admin / Admin@123**

## 解决方案结构

```
BeaverX.Admin/
├── BeaverX.Admin.Http.Host/             # 启动入口、appsettings、Serilog、JWT/CORS
├── BeaverX.Admin.Http.Api/              # Controller、鉴权 Filter
├── BeaverX.Admin.Infrastructure/        # MinIO、CAP、JWT 签发、密码哈希等技术实现
├── BeaverX.Admin.Application/           # AppService、Seeder、业务编排
├── BeaverX.Admin.Application.Contracts/ # DTO、IAppService、基础设施接口
├── BeaverX.Admin.Domain/                # 实体、IDataSeeder
├── BeaverX.Admin.Domain.Shared/         # 权限码、枚举等共享常量
└── BeaverX.Admin.EntityFrameworkCore/   # DbContext、Migrations
```

### 分层职责

| 层 | 职责 | 示例 |
|----|------|------|
| Domain | 实体、领域规则 | `SysConfig`、`Menu` |
| Domain.Shared | 跨层常量 | `RbacPermissionCodes` |
| Application.Contracts | 对外契约 | `IConfigAppService`、`IBlobStorage`、`IJwtTokenService` |
| Application | 业务实现 | `ConfigAppService`、`ExportTaskMessageService` |
| Infrastructure | 技术细节实现 | `MinioBlobStorage`、`JwtTokenService`、`ExportTaskCapSubscriber` |
| EntityFrameworkCore | 持久化 | `AdminDbContext`、迁移 |
| Http.Api | HTTP 适配 | `ConfigController` |
| Http.Host | 组合根、中间件 | JWT Bearer 校验、CORS、模块注册 |

### 依赖注入约定

实现 `IScopedDependency`（或 `ITransientDependency` / `ISingletonDependency`）的类会被 BeaverX 自动注册。AppService 同时实现业务接口即可被 Controller 注入。

## API 约定

- 路由前缀：`/api/[Controller]`（继承 `BeaverXController`）
- 权限：Controller 方法标注 `[RequirePermission("system:xxx:yyy")]`
- 权限码定义：`BeaverX.Admin.Domain.Shared/Rbac/RbacPermissionCodes.cs`
- 业务异常：抛出 `BusinessException`（`Domain.Shared`），由 `BusinessExceptionFilter` 统一返回 JSON

## 配置说明

| 配置节 | 文件 | 说明 |
|--------|------|------|
| `ConnectionStrings:Default` | appsettings.Development.json | PostgreSQL（`master`）或 MySQL（`master-mysql`） |
| `Jwt` | appsettings.json | 签发与校验 |
| `CorsOrgins` | appsettings.Development.json | 前端源，逗号分隔 |
| `Minio` | appsettings.json | 文件服务（可不配） |
| `Cache` | appsettings.json | 缓存驱动（Memory/Redis）、键前缀、默认 TTL |
| `Serilog` | appsettings.json | 日志级别与文件路径 `Logs/log-*.txt` |

## 数据库迁移

```bash
# 新增迁移
dotnet ef migrations add <MigrationName> \
  --project BeaverX.Admin.EntityFrameworkCore \
  --startup-project BeaverX.Admin.Http.Host

# 更新数据库
dotnet ef database update \
  --project BeaverX.Admin.EntityFrameworkCore \
  --startup-project BeaverX.Admin.Http.Host

# 回滚到指定迁移
dotnet ef database update <PreviousMigrationName> \
  --project BeaverX.Admin.EntityFrameworkCore \
  --startup-project BeaverX.Admin.Http.Host
```

新增实体后记得在 `AdminDbContext.OnModelCreating` 中配置表名、索引、字段长度。

## 新增业务模块（标准流程）

以「系统配置」为例，建议按以下顺序开发。

### 1. 领域实体

`BeaverX.Admin.Domain/Config/SysConfig.cs`，继承 `FullAuditedEntity`。

### 2. DbContext

`AdminDbContext` 增加 `DbSet<SysConfig>` 与 `OnModelCreating` 配置，然后执行迁移。

### 3. 权限码

`RbacPermissionCodes.cs` 增加：

```csharp
public static class Config
{
    public const string List = "system:config:list";
    public const string Create = "system:config:create";
    // ...
}
```

### 4. 契约层

- `Application.Contracts/Config/Dtos/ConfigDtos.cs`
- `Application.Contracts/Config/IConfigAppService.cs`

### 5. 应用服务

`Application/Config/ConfigAppService.cs`：

- 实现 `IConfigAppService` + `IScopedDependency`
- 使用 `IRepository<T>` 访问数据
- 校验失败抛 `BusinessException`

### 6. Controller

`Http.Api/Controllers/ConfigController.cs`：

```csharp
public class ConfigController : BeaverXController
{
    [RequirePermission(RbacPermissionCodes.System.Config.List)]
    [HttpGet("list")]
    public Task<PagedResultDto<ConfigDto>> GetListAsync(...) => ...;
}
```

### 7. 菜单与种子

- `ConfigMenuSeeder`：写入菜单、`path`、`component`、按钮权限，并赋给 `super_admin`
- `ConfigDataSeeder`（可选）：演示数据
- 实现 `IDataSeeder` + `IScopedDependency` 即可被 `DataSeederHostService` 自动执行

菜单字段需与前端约定一致：

| 字段 | 示例 | 说明 |
|------|------|------|
| `Path` | `/system/config` | 路由地址，**可自定义** |
| `Component` | `system/config/index` | 须与 `views/system/config/index.vue` 对应，用于前端匹配 |
| `Perms` | `system:config:list` | 页面访问权限 |

前端根据 `Component` 匹配页面，根据 `Path` 注册/展示路由地址；二者职责分离。

### 8. 前端联调

参考 [beaverx-vue-admin README](https://github.com/hdonghua/beaverx-vue-admin)：

1. 在 `router/routes/modules/` 增加静态路由与 `views/` 页面
2. 在菜单管理（或种子）中配置相同 `Component`，`Path` 可按需填写
3. 在 `constants/permissions.ts` 增加与 `RbacPermissionCodes` 一致的权限常量（供 `v-permission` 使用）
4. 为角色分配菜单后联调

**component 不一致** 是最常见的 403 原因（例如后端 `system/configs/index`，前端视图是 `system/config/index`）。

## RBAC 要点

- 超级管理员角色编码：`super_admin`，拥有全部菜单权限（查询与分配时自动全量）
- 菜单类型：目录 / 菜单 / 按钮；按钮 `IsVisible = false`，用于接口权限
- 隐藏菜单：`IsVisible = false` 的菜单不在侧边栏显示，但授权后仍可访问路由

## 站内信（管理端发送）

| API | 权限 | 说明 |
|-----|------|------|
| `POST /api/SiteMessageAdmin/send` | `system:message:send` | 向指定用户或全部启用用户发送站内信 |

发送走通用 `IMessageSender` → `site` 渠道（`SiteMessageChannelSender`），写入 `user_messages` 并通过 SignalR 推送 `message.unread.changed`。

前端页面：`/system/message`（发送站内信），菜单由 `MessageMenuSeeder` 在启动时种子写入。

## 实时通知（SignalR）

导出任务与未读消息通过 SignalR 推送，替代前端轮询。

| 组件 | 说明 |
|------|------|
| `IRealtimeNotifier` | 通用推送接口（Contracts） |
| `SignalRRealtimeNotifier` | SignalR 实现（Infrastructure） |
| `RealtimePublisher` | 业务编排：组装 payload 并推送 |
| `AdminNotificationHub` | Hub 地址 `/hubs/notifications` |

### 事件

| 事件名 | 触发时机 | Payload |
|--------|----------|---------|
| `export.task.changed` | 创建/认领/完成/失败 | `{ task, activeCount }` |
| `message.unread.changed` | 标为已读等 | `{ unreadCount }` |

客户端连接时携带 JWT（`accessTokenFactory` 或 query `access_token`），服务端按 `ClaimTypes.NameIdentifier` 定向推送到用户。

## 消息发送（多渠道）

站内信读取/已读 API 由 `IMessageAppService` 提供；**发送**统一走 `IMessageSender`，便于后续扩展钉钉、企微等渠道。

| 组件 | 说明 |
|------|------|
| `IMessageSender` | 通用发送门面（Contracts） |
| `IMessageChannelSender` | 单渠道发送器接口 |
| `MessageSender` | 按渠道分发（Application） |
| `MessageChannelRegistry` | 渠道注册表 |
| `SiteMessageChannelSender` | 站内信：写 `user_messages` + 推送未读数 |

### 渠道常量

`MessageChannels`：`site`（站内信）、`dingtalk`、`wecom`（预留）。

### 使用示例

业务代码注入 `IMessageSender`：

```csharp
await _messageSender.SendAsync(new SendMessageRequest
{
    UserId = userId,
    Type = UserMessageTypes.Notice,
    Title = "导出完成",
    Content = "您的导出任务已完成，请前往下载。",
    Channels = [MessageChannels.Site]  // 省略则发送到全部已注册渠道
}, cancellationToken);
```

### 扩展新渠道

1. 在 `Infrastructure`（或独立包）实现 `IMessageChannelSender`，声明 `Channel` 常量
2. 实现 `IScopedDependency` 即可被 DI 自动注册
3. 调用方通过 `Channels` 指定渠道，或默认广播到全部已注册渠道

## 定时任务（Hangfire）

基于 **Hangfire + PostgreSQL / MySQL**（随分支：`master` / `master-mysql`），支持两种互不冲突的周期性任务：

| 方式 | 说明 | Hangfire Job Id |
|------|------|-----------------|
| **后台 HTTP API 任务** | 管理端「系统管理 → 定时任务」或 `POST /api/ScheduledJob` 配置，定时请求 HTTP URL | `scheduled-job:{id}` |
| **代码 `IRecurringJob`** | 实现接口并注册 DI，启动时自动同步到 Hangfire | 类型全名 |

### 方式一：后台 HTTP API 任务

- 数据表：`sys_scheduled_jobs`、`sys_scheduled_job_logs`
- 前端页面：`/system/job`（权限 `system:job:*`）
- 创建/更新后由 `IHangfireScheduledJobRegistrar` 同步 Hangfire；支持手动触发、Cron 校验、执行日志
- 当前 `JobType` 仅支持 **HttpApi**（GET/POST/PUT/DELETE）

```http
POST /api/ScheduledJob
{
  "jobCode": "health-check",
  "name": "健康检查",
  "jobType": 1,
  "cronExpression": "0 */5 * * *",
  "httpMethod": 1,
  "httpUrl": "http://localhost:5216/api/Health",
  "timeoutSeconds": 30
}
```

### 方式二：代码 `IRecurringJob`

实现 `IRecurringJob`（含 `CronExpression` + `ExecuteAsync`），继承 `IScopedDependency` 即可被 DI 扫描；`CodeRecurringJobSyncHostedService` 启动时注册。

```csharp
public class SampleDailyRecurringJob : IRecurringJob
{
    public string CronExpression => "0 0 * * *";

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // 注入 IRepository / IAppService 执行业务
        return Task.CompletedTask;
    }
}
```

参考：`Application/Scheduling/Jobs/SampleDailyRecurringJob.cs`

### 配置与 Dashboard

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

- Dashboard：`/hangfire`（HTTP Basic，与业务 JWT 无关）
- 多实例：Hangfire 使用数据库持久化（PostgreSQL 或 MySQL），多节点可同时跑 Worker，**任务逻辑需幂等**

保姆级说明见配套文档 `doc-beaverx-admin/docs/backend/scheduled-jobs.md`。

## 异步导出（DotNetCap）

导出任务采用 **CAP + 数据库存储（PostgreSQL / MySQL，随分支）+ 内存队列 + MinIO 文件**：

| 组件 | 说明 |
|------|------|
| `export_tasks` | 业务任务表（状态、参数、文件链接） |
| `local_message_outbox` | CAP 消息消费去重，仅记录 `cap_message_id`（同一消息只成功消费一次） |
| `cap` schema | CAP 自带的 published / received 消息表 |
| `ExportTaskCapSubscriber`（Infrastructure） | CAP 消费者，生成 Excel（内存流）后上传 MinIO |

### 流程

1. `POST /api/ExportTask` 写入 `export_tasks`，发布 CAP 消息
2. `ICapPublisher` 发布 `export.task.execute` 消息
3. 消费者校验 `cap_message_id` 未消费 → 认领任务（`Pending → Processing`）→ 导出 → 上传 MinIO → `Completed` → 记录 `cap_message_id`
4. 启动时 `ExportTaskRecoveryHostedService` 恢复中断的 Pending 任务

### 幂等策略

- **CAP 消费层**（`CapMessageConsumeService`）：成功后写入 `local_message_outbox.cap_message_id`，CAP 重投同一消息时直接跳过
- **业务层**（如 `ExportTaskMessageService`）：`export_tasks.Status` 原子认领（`Pending → Processing`），已完成/进行中任务不再处理
- **重试**：失败时任务回滚为 `Pending`，由 CAP 自动重试（最多 5 次）；`cap_message_id` 仅在成功后才写入

新增异步 CAP 消费者时：业务自行保证幂等，成功后调用 `CapMessageConsumeService.MarkConsumedAsync(capMessageId)` 即可。

### 扩展导出类型

实现 `IExportHandler` 并注册 `ExportType` 常量，前端传对应 `exportType` 与 `parameters` 即可。

### 生产部署

当前使用 `Savorboard.CAP.InMemoryMessageQueue`（单实例）。多实例部署请改用 Redis / RabbitMQ 等 CAP 传输。

## 缓存

通用缓存通过 `ICacheService`（Contracts）+ `CacheService`（Infrastructure）提供，支持 **Memory** / **Redis** 驱动切换。

### 配置

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

| 字段 | 说明 |
|------|------|
| `Driver` | `Memory`（默认，单实例）或 `Redis`（多实例共享） |
| `KeyPrefix` | 全局键前缀，如 `beaverx:admin:` |
| `RedisConnectionString` | Redis 连接串；未配置时回退 `ConnectionStrings:Redis` |
| `DefaultExpirationSeconds` | `SetAsync` 未指定过期时间时的默认 TTL |

### 使用

在 AppService 中注入 `ICacheService`：

```csharp
var user = await _cache.GetOrSetAsync(
    $"user:{id}",
    ct => LoadUserFromDbAsync(id, ct),
    TimeSpan.FromMinutes(10),
    cancellationToken);
```

业务代码只写逻辑键（如 `user:1`），前缀由配置统一拼接。

## 多节点部署

默认配置面向**单实例**开发/小规模部署。水平扩展（多 Pod / 多进程）时，以下组件需改为共享存储或分布式中间件：

| 能力 | 单实例（默认） | 多节点需调整 |
|------|----------------|--------------|
| 业务缓存 `ICacheService` | `Cache:Driver = Memory` | 改为 `Redis`，并配置 `RedisConnectionString` |
| SignalR 实时推送 | 本机 Hub 连接 | 增加 **Redis Backplane**，否则 `SendToUser` 只能命中本节点连接 |
| 在线用户 `IOnlineUserTracker` | 内存 `OnlineUserTracker` | 启用 **`RedisOnlineUserTracker`**（基于 StackExchange.Redis `IDatabase`） |
| CAP 异步导出 | `UseInMemoryMessageQueue()` | 改为 Redis / RabbitMQ 等共享队列 |
| Hangfire | 数据库持久化（PostgreSQL / MySQL） | 多实例可同时跑 Worker，注意任务幂等 |
| JWT / 数据库 / MinIO | 无节点亲和 | 一般无需改动 |

### 1. 缓存切 Redis

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

在 `BeaverXAdminInfrastructureModule` 中为 `AddSignalR()` 追加 Backplane（需引用 `Microsoft.AspNetCore.SignalR.StackExchangeRedis`）：

```csharp
using BeaverX.Admin.Infrastructure.Realtime;

var redisConnection = RealtimeDistributedExtensions.ResolveRedisConnectionString(configuration);

services.AddSignalR()
    .AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("BeaverXAdmin:SignalR:");
    })
    .AddJsonProtocol(/* 保持现有 JSON 配置 */);
```

前端仍连接负载均衡后的同一 Hub 地址 `/hubs/notifications`；Backplane 负责跨节点转发消息。

### 3. 在线用户：RedisOnlineUserTracker

实现位于 `Infrastructure/Realtime/RedisOnlineUserTracker.cs`，通过注入的 **`IDatabase`** 读写 Redis Hash（键：`{KeyPrefix}online:connections`），**不**使用 `IDistributedCache`。

**默认不启用**。在 `BeaverXAdminHttpHostModule.ConfigureServices` 中、于 Infrastructure 模块加载**之后**调用：

```csharp
using BeaverX.Admin.Infrastructure.Realtime;

public override void ConfigureServices(ServiceConfigurationContext context)
{
    // ... 现有 JWT、CORS 等 ...

    context.Services.AddRedisOnlineUserTracker(context.Configuration);
}
```

扩展方法 `AddRedisOnlineUserTracker` 会：

1. 注册 `IConnectionMultiplexer` 与 `IDatabase`（连接串读取 `Cache:RedisConnectionString` 或 `ConnectionStrings:Redis`）
2. 用 `RedisOnlineUserTracker` **替换**默认的 `OnlineUserTracker` 注册

若仅需自定义 Redis 连接、仍使用分布式追踪，也可手动注册：

```csharp
services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(configuration["Cache:RedisConnectionString"]!));
services.AddSingleton(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
services.Replace(ServiceDescriptor.Singleton<IOnlineUserTracker, RedisOnlineUserTracker>());
```

### 4. CAP 消息队列

在 `BeaverXAdminInfrastructureModule.ConfigureCap` 中，将 `UseInMemoryMessageQueue()` 替换为例如：

```csharp
// 需引用 DotNetCore.CAP.RedisStreams 或 DotNetCore.CAP.RabbitMQ 等
options.UseRedis(redisConnectionString);
```

否则导出任务消息只会被发布到本进程内存队列，其他节点无法消费。

### 5. 推荐检查清单

- [ ] 数据库（PostgreSQL 或 MySQL）、Redis、MinIO 对所有 API 实例可达
- [ ] `Cache:Driver = Redis`
- [ ] SignalR 已配置 Redis Backplane
- [ ] Host 模块已调用 `AddRedisOnlineUserTracker`
- [ ] CAP 已改用共享队列
- [ ] 负载均衡开启 WebSocket 粘性会话**或**依赖 Backplane（推荐后者，粘性非必须）
- [ ] 各实例 `Jwt:SecretKey`、CORS 配置一致

## 日志

- 控制台 + `BeaverX.Admin.Http.Host/Logs/log-YYYYMMDD.txt`
- 开发环境可在 `appsettings.Development.json` 覆盖 `Serilog:MinimumLevel`
- HTTP 请求日志：`UseSerilogRequestLogging()`

## 常见问题

| 现象 | 排查 |
|------|------|
| 迁移失败 | 连接串是否正确；是否指定 `--startup-project` |
| 启动后无种子数据 | 检查 `IDataSeeder` 是否实现；表是否已有数据（种子幂等跳过） |
| 前端 403 | 角色是否分配菜单；`Component` 是否与 `views/` 一致；权限码是否与 Controller 一致 |
| CORS 错误 | `CorsOrgins` 是否包含前端地址 |
| MinIO 相关错误 | 导出/上传依赖 MinIO，请确认服务与配置 |
| 导出一直 Pending | 检查 CAP 是否启动；查看 `cap` schema 与 `Logs/` |

## 相关仓库

- 管理后台前端：[beaverx-vue-admin](https://github.com/hdonghua/beaverx-vue-admin)

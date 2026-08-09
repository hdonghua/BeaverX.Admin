# BeaverX.Admin（后端）

> **Language**: 简体中文 | [English](README.en.md)

基于 [ABP Framework](https://abp.io/)（`Volo.Abp.*`）的 ASP.NET Core 管理后台 API。实体主键统一为 **Guid**。

前端：[beaverx-vue-admin](https://github.com/hdonghua/beaverx-vue-admin)

> **分支说明**：本仓库以 `master`（EF Core + PostgreSQL）为唯一持续维护分支。其它beaverx-xxx分支**不再更新**。

## 实现功能

| 能力 | 说明 |
|------|------|
| **实时消息** | SignalR + Redis Backplane：未读站内信、导出进度、在线用户、强制下线等实时推送；按浏览器设备指纹聚合在线态，心跳 + TTL 自动离线 |
| **异步导出** | CAP + Redis Streams：创建导出任务后异步生成 Excel 并上传 MinIO，前端经 SignalR 感知状态变化，支持幂等消费与失败重试 |
| **完整工作流** | 流程设计 / 表单设计、发起与审批、转交 / 加签 / 减签 / 回退 / 催办 / 撤销、抄送与打印、服务任务节点等 OA 审批能力 |

另含 RBAC、字典、系统配置、支付渠道、工单、定时任务（Hangfire）等管理后台常用模块。

## 技术栈


| 类别           | 技术                                                    |
| ------------ | ----------------------------------------------------- |
| 运行时          | .NET 10                                               |
| Web          | ASP.NET Core + Volo.Abp.AspNetCore.Mvc                |
| ORM          | Entity Framework Core + PostgreSQL                    |
| 主键           | Guid  |
| 认证           | JWT Bearer + Refresh Token             |
| 缓存 / 实时 / 消息 | Redis（分布式缓存、SignalR Backplane、CAP Redis Streams、在线用户） |
| 定时任务         | Hangfire（PostgreSQL 存储）                               |
| 日志           | Serilog（控制台 + 本地文件）                                   |
| 对象存储         | MinIO（可选）                                             |


**环境要求**：[.NET 10 SDK](https://dotnet.microsoft.com/download)、PostgreSQL 14+、Redis 6+；（可选）MinIO。

## 数据库迁移

配置 `BeaverX.Admin.Http.Host/appsettings.Development.json` 中的 `ConnectionStrings:Default` 后执行：

首次启动前：

```bash
# 切换到BeaverX.Admin.EntityFrameworkCore目录，打开终端，执行以下命令

dotnet ef database update
```

默认地址见 `Properties/launchSettings.json`（一般为 `http://localhost:5216`）。种子数据含默认管理员：**admin / Admin@123**。

## 各层职责

```
BeaverX.Admin/
├── BeaverX.Admin.Http.Host/             # 启动入口、appsettings、中间件
├── BeaverX.Admin.Http.Api/              # Controller、鉴权 Filter
├── BeaverX.Admin.Infrastructure/        # JWT、MinIO、CAP、Hangfire、SignalR 等
├── BeaverX.Admin.Application/           # AppService、Seeder、业务编排
├── BeaverX.Admin.Application.Contracts/ # DTO、应用/基础设施接口
├── BeaverX.Admin.Domain/                # 实体、领域规则
├── BeaverX.Admin.Domain.Shared/         # 权限码、枚举等共享常量
└── BeaverX.Admin.EntityFrameworkCore/   # DbContext、Migrations
```


| 层                     | 职责      | 示例                                          |
| --------------------- | ------- | ------------------------------------------- |
| Domain                | 实体、领域规则 | `SysConfig`、`Menu`                          |
| Domain.Shared         | 跨层常量    | `RbacPermissionCodes`                       |
| Application.Contracts | 对外契约    | `IConfigAppService`、`IBlobStorage`          |
| Application           | 业务实现    | `ConfigAppService`、`AuthAppService`         |
| Infrastructure        | 技术实现    | `JwtTokenService`、`ExportTaskCapSubscriber` |
| EntityFrameworkCore   | 持久化     | `AdminDbContext`、迁移                         |
| Http.Api              | HTTP 适配 | `ConfigController`                          |
| Http.Host             | 组合根     | 模块注册、JWT / CORS                             |


实现 `IScopedDependency` / `ITransientDependency` / `ISingletonDependency` 的类型由 ABP 自动注册。

## 配置说明

本地开发配置集中在 `BeaverX.Admin.Http.Host/appsettings.Development.json`：

| 配置节 | 说明 |
|--------|------|
| `ConnectionStrings:Default` | PostgreSQL 连接串 |
| `Cache:RedisConnectionString` | Redis（缓存 / SignalR / CAP / 刷新令牌 / 在线用户），必填 |
| `Cache:KeyPrefix` / `DefaultExpirationSeconds` | 缓存键前缀与默认 TTL |
| `Jwt` | 签发与校验（Issuer、Audience、SecretKey、过期时间） |
| `CorsOrgins` | 前端源，逗号分隔 |
| `Minio` | 对象存储（可选；导出/上传依赖时需配置） |
| `Hangfire` | Schema、Dashboard、启动同步策略 |
| `Payment` | 支付回调与证书路径等 |
| `Serilog` | 日志级别与 `Logs/log-*.txt` |
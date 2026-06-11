namespace BeaverX.Admin.Infrastructure.Scheduling;

/// <summary>
/// 服务启动时，业务表（sys_scheduled_jobs）与 Hangfire 周期性任务的同步策略。
/// </summary>
public enum BusinessJobStartupSyncMode
{
    /// 始终以数据库为准，覆盖 Hangfire（默认）。
    ApplyDatabase = 0,

    /// 若 Hangfire 中已有 scheduled-job:{id}，以其 Cron 回写数据库后再注册。
    MergeFromHangfire = 1,
}

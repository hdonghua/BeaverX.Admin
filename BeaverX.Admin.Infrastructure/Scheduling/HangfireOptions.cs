namespace BeaverX.Admin.Infrastructure.Scheduling;

public class HangfireOptions
{
    public const string SectionName = "Hangfire";

    public string SchemaName { get; set; } = "hangfire";

    public bool EnableDashboard { get; set; } = true;

    public string DashboardPath { get; set; } = "/hangfire";

    /// <summary>
    /// 启动时是否将 sys_scheduled_jobs 同步到 Hangfire。
    /// </summary>
    public bool SyncBusinessJobsOnStartup { get; set; } = true;

    /// <summary>
    /// 业务任务启动同步策略（仅当 SyncBusinessJobsOnStartup 为 true 时生效）。
    /// </summary>
    public BusinessJobStartupSyncMode BusinessJobStartupSyncMode { get; set; } =
        BusinessJobStartupSyncMode.ApplyDatabase;

    /// <summary>
    /// Dashboard 独立 Basic 认证（浏览器弹窗输入账号密码）。
    /// </summary>
    public HangfireDashboardAuthOptions Auth { get; set; } = new();
}

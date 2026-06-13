using BeaverX.Core.Dependency;

namespace BeaverX.Admin.Application.Contracts.Scheduling;

/// <summary>
/// 代码定义的 Hangfire 周期性任务。实现此接口并注册到 DI 后，启动时将以类型全名作为 Job Id 自动注册。
/// </summary>
public interface IRecurringJob : IScopedDependency
{
    /// <summary>
    /// Hangfire Cron 表达式（标准五段或六段格式）。
    /// </summary>
    string CronExpression { get; }

    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

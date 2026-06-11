using System.Linq.Expressions;
using Hangfire;
using Hangfire.Storage;

namespace BeaverX.Admin.Infrastructure.Scheduling;

/// <summary>
/// 启动时注册周期性任务的辅助方法。
/// Hangfire 的 <see cref="RecurringJob.AddOrUpdate"/> 每次都会覆盖 Cron，
/// 面板修改后若启动时再次调用会被还原。
/// </summary>
public static class HangfireRecurringJobStartup
{
  public static bool Exists(string recurringJobId)
  {
    using var connection = JobStorage.Current.GetConnection();
    return connection.GetRecurringJobs()
      .Any(x => string.Equals(x.Id, recurringJobId, StringComparison.Ordinal));
  }

  /// <summary>
  /// 仅当 Hangfire 中不存在该任务时才注册（保留面板中已修改的 Cron）。
  /// </summary>
  public static void AddOrUpdateIfNotExists(
    string recurringJobId,
    Expression<Action> methodCall,
    string cronExpression,
    RecurringJobOptions? options = null)
  {
    if (Exists(recurringJobId))
    {
      return;
    }

    RecurringJob.AddOrUpdate(recurringJobId, methodCall, cronExpression, options);
  }
}

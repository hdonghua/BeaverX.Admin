using BeaverX.Admin.Application.Contracts.Scheduling;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Scheduling.Jobs;

/// <summary>
/// 示例：每天 UTC 0 点执行。新增任务只需实现 <see cref="IRecurringJob"/>
/// </summary>
public class SampleDailyRecurringJob : IRecurringJob
{
    private readonly ILogger<SampleDailyRecurringJob> _logger;

    public SampleDailyRecurringJob(ILogger<SampleDailyRecurringJob> logger)
    {
        _logger = logger;
    }

    public string CronExpression => "0 0 * * *";

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sample daily recurring job executed at {Time:O}", DateTime.UtcNow);
        return Task.CompletedTask;
    }
}

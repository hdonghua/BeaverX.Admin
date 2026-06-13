using BeaverX.Admin.Application.Contracts.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Infrastructure.Scheduling;

/// <summary>
/// Hangfire 执行入口：按任务类型全名从 DI 解析 <see cref="IRecurringJob"/> 并执行。
/// </summary>
public class CodeRecurringJobRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CodeRecurringJobRunner> _logger;

    public CodeRecurringJobRunner(
        IServiceScopeFactory scopeFactory,
        ILogger<CodeRecurringJobRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(string jobTypeFullName, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var job = scope.ServiceProvider
            .GetServices<IRecurringJob>()
            .FirstOrDefault(x => string.Equals(x.GetType().FullName, jobTypeFullName, StringComparison.Ordinal));

        if (job == null)
        {
            _logger.LogWarning("Recurring job {JobType} is not registered in DI", jobTypeFullName);
            return;
        }

        _logger.LogDebug("Executing recurring job {JobType}", jobTypeFullName);
        await job.ExecuteAsync(cancellationToken);
    }
}

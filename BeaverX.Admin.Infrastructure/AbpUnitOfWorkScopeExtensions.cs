using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Uow;

namespace BeaverX.Admin.Infrastructure;

public static class AbpUnitOfWorkScopeExtensions
{
    /// <summary>
    /// Creates a DI scope and an ABP unit of work for background / hosted service code that uses repositories.
    /// </summary>
    public static async Task RunInUnitOfWorkAsync(
        this IServiceScopeFactory scopeFactory,
        Func<IServiceProvider, CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        using var uow = uowManager.Begin(requiresNew: true, isTransactional: true);
        await action(scope.ServiceProvider, cancellationToken);
        await uow.CompleteAsync(cancellationToken);
    }
}

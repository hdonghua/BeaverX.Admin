using Volo.Abp.Uow;

namespace BeaverX.Admin.Application;

/// <summary>
/// Compatibility helper mapping BeaverX-style ExecuteAsync onto ABP IUnitOfWorkManager.
/// </summary>
public static class UnitOfWorkExecuteExtensions
{
    public static async Task ExecuteAsync(
        this IUnitOfWorkManager unitOfWorkManager,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
        await action(cancellationToken);
        await uow.CompleteAsync(cancellationToken);
    }
}

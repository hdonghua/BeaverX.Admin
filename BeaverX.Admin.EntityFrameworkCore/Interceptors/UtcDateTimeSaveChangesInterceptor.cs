using BeaverX.Admin.Domain.Shared.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BeaverX.Admin.EntityFrameworkCore.Interceptors;

/// <summary>
/// 将写入数据库的 DateTime 统一转为 UTC，兼容 BeaverX 审计字段使用的 DateTime.Now（Local）。
/// </summary>
public class UtcDateTimeSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        NormalizeDateTimes(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        NormalizeDateTimes(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void NormalizeDateTimes(DbContext? context)
    {
        if (context == null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Detached)
            {
                continue;
            }

            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dateTime)
                {
                    property.CurrentValue = ToUtc(dateTime);
                }
            }
        }
    }

    internal static DateTime ToUtc(DateTime value) => DateTimeUtcHelper.ToUtc(value);
}

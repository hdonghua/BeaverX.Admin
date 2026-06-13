using BeaverX.Admin.Application.Contracts.Rbac;
using Cronos;

namespace BeaverX.Admin.Application.Scheduling;

public static class CronExpressionHelper
{
    public static void EnsureValid(string cronExpression)
    {
        var error = TryParse(cronExpression, out _);
        if (error != null)
        {
            throw new BusinessException(error);
        }
    }

    public static string? TryParse(string cronExpression, out CronExpression? expression)
    {
        expression = null;
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return "Cron 表达式不能为空";
        }

        var trimmed = cronExpression.Trim();
        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var format = parts.Length switch
        {
            5 => CronFormat.Standard,
            6 => CronFormat.IncludeSeconds,
            _ => (CronFormat?)null
        };

        if (format == null)
        {
            return "Cron 表达式必须为 5 段（分 时 日 月 周）或 6 段（秒 分 时 日 月 周）";
        }

        var cronFormat = format.Value;
        try
        {
            expression = CronExpression.Parse(trimmed, cronFormat);
            return null;
        }
        catch (Exception ex)
        {
            return $"Cron 表达式无效: {ex.Message}";
        }
    }

    public static List<DateTime> GetNextOccurrences(string cronExpression, int count = 5)
    {
        var error = TryParse(cronExpression, out var expression);
        if (error != null || expression == null)
        {
            return [];
        }

        var results = new List<DateTime>();
        var cursor = DateTime.UtcNow;
        for (var i = 0; i < count; i++)
        {
            var next = expression.GetNextOccurrence(cursor, TimeZoneInfo.Utc);
            if (next == null)
            {
                break;
            }

            results.Add(next.Value);
            cursor = next.Value.AddSeconds(1);
        }

        return results;
    }
}

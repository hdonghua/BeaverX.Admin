using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Contracts.Scheduling;
using BeaverX.Admin.Application.Contracts.Scheduling.Dtos;
using BeaverX.Admin.Application.Rbac;
using BeaverX.Admin.Domain.Scheduling;
using BeaverX.Admin.Domain.Shared.Scheduling;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Scheduling;

public class ScheduledJobAppService : IScheduledJobAppService, IScopedDependency
{
    private readonly IRepository<ScheduledJob> _jobRepository;
    private readonly IRepository<ScheduledJobLog> _logRepository;
    private readonly IHangfireScheduledJobRegistrar _registrar;

    public ScheduledJobAppService(
        IRepository<ScheduledJob> jobRepository,
        IRepository<ScheduledJobLog> logRepository,
        IHangfireScheduledJobRegistrar registrar)
    {
        _jobRepository = jobRepository;
        _logRepository = logRepository;
        _registrar = registrar;
    }

    public async Task<PagedResultDto<ScheduledJobDto>> GetListAsync(
        ScheduledJobQueryDto input,
        CancellationToken cancellationToken = default)
    {
        var query = _jobRepository.GetQueryable().AsQueryable();

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            query = query.Where(x =>
                x.Name.Contains(keyword) ||
                x.JobCode.Contains(keyword) ||
                (x.Description != null && x.Description.Contains(keyword)));
        }

        if (input.IsEnabled.HasValue)
        {
            query = query.Where(x => x.IsEnabled == input.IsEnabled.Value);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var (skip, take) = RbacQueryHelper.GetPaging(input.Page, input.PageSize);
        var items = await query
            .OrderByDescending(x => x.CreationTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<ScheduledJobDto>
        {
            Total = total,
            Items = items.Select(ToDto).ToList()
        };
    }

    public async Task<ScheduledJobDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        return ToDto(entity);
    }

    public async Task<ScheduledJobDto> CreateAsync(
        CreateScheduledJobDto input,
        CancellationToken cancellationToken = default)
    {
        ValidateCreateInput(input);

        var jobCode = input.JobCode.Trim();
        if (await _jobRepository.AnyAsync(x => x.JobCode == jobCode, cancellationToken))
        {
            throw new BusinessException($"任务编码已存在: {jobCode}");
        }

        var entity = new ScheduledJob
        {
            JobCode = jobCode,
            Name = input.Name.Trim(),
            JobType = input.JobType,
            CronExpression = input.CronExpression.Trim(),
            TimeZoneId = NormalizeTimeZone(input.TimeZoneId),
            IsEnabled = input.IsEnabled,
            Description = input.Description?.Trim(),
            HttpMethod = input.HttpMethod,
            HttpUrl = input.HttpUrl.Trim(),
            HttpHeadersJson = input.HttpHeadersJson?.Trim(),
            HttpBody = input.HttpBody,
            TimeoutSeconds = input.TimeoutSeconds > 0 ? input.TimeoutSeconds : 30,
        };

        await _jobRepository.InsertAsync(entity, cancellationToken: cancellationToken);
        SyncHangfire(entity);

        return ToDto(entity);
    }

    public async Task<ScheduledJobDto> UpdateAsync(
        long id,
        UpdateScheduledJobDto input,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(input.Name))
        {
            entity.Name = input.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(input.CronExpression))
        {
            CronExpressionHelper.EnsureValid(input.CronExpression);
            entity.CronExpression = input.CronExpression.Trim();
        }

        if (!string.IsNullOrWhiteSpace(input.TimeZoneId))
        {
            entity.TimeZoneId = NormalizeTimeZone(input.TimeZoneId);
        }

        if (input.IsEnabled.HasValue)
        {
            entity.IsEnabled = input.IsEnabled.Value;
        }

        if (input.Description != null)
        {
            entity.Description = string.IsNullOrWhiteSpace(input.Description)
                ? null
                : input.Description.Trim();
        }

        if (input.HttpMethod.HasValue)
        {
            entity.HttpMethod = input.HttpMethod.Value;
        }

        if (!string.IsNullOrWhiteSpace(input.HttpUrl))
        {
            entity.HttpUrl = input.HttpUrl.Trim();
        }

        if (input.HttpHeadersJson != null)
        {
            entity.HttpHeadersJson = string.IsNullOrWhiteSpace(input.HttpHeadersJson)
                ? null
                : input.HttpHeadersJson.Trim();
        }

        if (input.HttpBody != null)
        {
            entity.HttpBody = string.IsNullOrWhiteSpace(input.HttpBody) ? null : input.HttpBody;
        }

        if (input.TimeoutSeconds.HasValue && input.TimeoutSeconds.Value > 0)
        {
            entity.TimeoutSeconds = input.TimeoutSeconds.Value;
        }

        await _jobRepository.UpdateAsync(entity, cancellationToken: cancellationToken);
        SyncHangfire(entity);

        return ToDto(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        _registrar.Remove(entity.Id);
        await _jobRepository.DeleteAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task TriggerAsync(long id, CancellationToken cancellationToken = default)
    {
        _ = await FindAsync(id, cancellationToken);
        _registrar.Enqueue(id);
    }

    public async Task<PagedResultDto<ScheduledJobLogDto>> GetLogsAsync(
        long id,
        ScheduledJobLogQueryDto input,
        CancellationToken cancellationToken = default)
    {
        _ = await FindAsync(id, cancellationToken);

        var query = _logRepository.GetQueryable()
            .Where(x => x.JobId == id)
            .AsQueryable();

        var total = await query.LongCountAsync(cancellationToken);
        var (skip, take) = RbacQueryHelper.GetPaging(input.Page, input.PageSize);
        var items = await query
            .OrderByDescending(x => x.StartedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<ScheduledJobLogDto>
        {
            Total = total,
            Items = items.Select(ToLogDto).ToList()
        };
    }

    public ValidateCronResultDto ValidateCron(ValidateCronDto input)
    {
        var error = CronExpressionHelper.TryParse(input.CronExpression, out _);
        return new ValidateCronResultDto
        {
            IsValid = error == null,
            ErrorMessage = error,
            NextOccurrences = error == null
                ? CronExpressionHelper.GetNextOccurrences(input.CronExpression)
                : null
        };
    }

    private static void ValidateCreateInput(CreateScheduledJobDto input)
    {
        if (string.IsNullOrWhiteSpace(input.JobCode))
        {
            throw new BusinessException("任务编码不能为空");
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new BusinessException("任务名称不能为空");
        }

        if (input.JobType != ScheduledJobType.HttpApi)
        {
            throw new BusinessException("当前仅支持 HTTP API 类型任务");
        }

        CronExpressionHelper.EnsureValid(input.CronExpression);

        if (string.IsNullOrWhiteSpace(input.HttpUrl))
        {
            throw new BusinessException("HTTP 地址不能为空");
        }

        if (!Uri.TryCreate(input.HttpUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new BusinessException("HTTP 地址格式无效");
        }
    }

    private void SyncHangfire(ScheduledJob entity)
    {
        _registrar.Register(new ScheduledJobRegistration
        {
            JobId = entity.Id,
            CronExpression = entity.CronExpression,
            TimeZoneId = entity.TimeZoneId,
            IsEnabled = entity.IsEnabled
        });
    }

    private async Task<ScheduledJob> FindAsync(long id, CancellationToken cancellationToken)
    {
        var entity = await _jobRepository.FindAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            throw new BusinessException($"定时任务不存在: {id}");
        }

        return entity;
    }

    private static string NormalizeTimeZone(string? timeZoneId)
    {
        var value = string.IsNullOrWhiteSpace(timeZoneId) ? "Asia/Shanghai" : timeZoneId.Trim();
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(value);
            return value;
        }
        catch (TimeZoneNotFoundException)
        {
            throw new BusinessException($"时区无效: {value}");
        }
    }

    private static ScheduledJobDto ToDto(ScheduledJob entity) => new()
    {
        Id = entity.Id,
        JobCode = entity.JobCode,
        Name = entity.Name,
        JobType = entity.JobType,
        CronExpression = entity.CronExpression,
        TimeZoneId = entity.TimeZoneId,
        IsEnabled = entity.IsEnabled,
        Description = entity.Description,
        HttpMethod = entity.HttpMethod,
        HttpUrl = entity.HttpUrl,
        HttpHeadersJson = entity.HttpHeadersJson,
        HttpBody = entity.HttpBody,
        TimeoutSeconds = entity.TimeoutSeconds,
        LastRunTime = entity.LastRunTime,
        LastRunStatus = entity.LastRunStatus,
        LastRunMessage = entity.LastRunMessage,
        CreationTime = entity.CreationTime
    };

    private static ScheduledJobLogDto ToLogDto(ScheduledJobLog entity) => new()
    {
        Id = entity.Id,
        JobId = entity.JobId,
        Status = entity.Status,
        StartedAt = entity.StartedAt,
        FinishedAt = entity.FinishedAt,
        DurationMs = entity.DurationMs,
        HttpStatusCode = entity.HttpStatusCode,
        ResponseBody = entity.ResponseBody,
        ErrorMessage = entity.ErrorMessage,
        IsManualTrigger = entity.IsManualTrigger
    };
}

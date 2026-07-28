using System.Text.Json;
using BeaverX.Admin.Application.Contracts.Exports;
using BeaverX.Admin.Application.Contracts.Exports.Dtos;
using BeaverX.Admin.Application.Realtime;
using BeaverX.Admin.Application.Contracts.Storage;
using BeaverX.Admin.Domain.Exports;
using BeaverX.Admin.Domain.Shared.Exports;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Exports;

public class ExportTaskAppService : IExportTaskAppService, IScopedDependency
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IRepository<ExportTask, Guid> _exportTaskRepository;
    private readonly ExportHandlerRegistry _handlerRegistry;
    private readonly IExportTaskPublisher _exportTaskPublisher;
    private readonly IBlobStorage _blobStorage;
    private readonly RealtimePublisher _realtimePublisher;
    private readonly ICurrentUser _currentUser;

    public ExportTaskAppService(
        IRepository<ExportTask, Guid> exportTaskRepository,
        ExportHandlerRegistry handlerRegistry,
        IExportTaskPublisher exportTaskPublisher,
        IBlobStorage blobStorage,
        RealtimePublisher realtimePublisher,
        ICurrentUser currentUser)
    {
        _exportTaskRepository = exportTaskRepository;
        _handlerRegistry = handlerRegistry;
        _exportTaskPublisher = exportTaskPublisher;
        _blobStorage = blobStorage;
        _realtimePublisher = realtimePublisher;
        _currentUser = currentUser;
    }

    public async Task<ExportTaskDto> CreateAsync(
        CreateExportTaskDto input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.ExportType))
        {
            throw new BusinessException("导出类型不能为空");
        }

        var exportType = input.ExportType.Trim();
        var handler = _handlerRegistry.GetRequired(exportType);
        var userId = GetCurrentUserId();
        var parametersJson = input.Parameters == null
            ? null
            : JsonSerializer.Serialize(input.Parameters, JsonOptions);

        var entity = new ExportTask
        {
            UserId = userId,
            ExportType = exportType,
            Parameters = parametersJson,
            FileName = $"{handler.DisplayName}_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
            Status = ExportTaskStatus.Pending
        };

        await _exportTaskRepository.InsertAsync(entity, cancellationToken: cancellationToken);
        await _exportTaskPublisher.PublishExecuteAsync(entity.Id, cancellationToken);
        await _realtimePublisher.NotifyExportTaskChangedAsync(entity, cancellationToken);

        return ToDto(entity);
    }

    public async Task<List<ExportTaskDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var items = await (await _exportTaskRepository.GetQueryableAsync())
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreationTime)
            .Take(50)
            .ToListAsync(cancellationToken);

        return items.Select(ToDto).ToList();
    }

    public async Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var count = await (await _exportTaskRepository.GetQueryableAsync())
            .LongCountAsync(
                x => x.UserId == userId &&
                     (x.Status == ExportTaskStatus.Pending || x.Status == ExportTaskStatus.Processing),
                cancellationToken);

        return (int)count;
    }

    public async Task<ExportDownloadUrlDto> GetDownloadUrlAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindOwnedTaskAsync(id, cancellationToken);
        if (entity.Status != ExportTaskStatus.Completed || string.IsNullOrWhiteSpace(entity.ObjectKey))
        {
            throw new BusinessException("导出文件尚未就绪");
        }

        var url = await _blobStorage.GetPresignedUrlAsync(entity.ObjectKey, cancellationToken: cancellationToken);
        entity.FileUrl = url;
        await _exportTaskRepository.UpdateAsync(entity, cancellationToken: cancellationToken);

        return new ExportDownloadUrlDto
        {
            Url = url,
            FileName = entity.FileName
        };
    }

    private async Task<ExportTask> FindOwnedTaskAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var entity = await _exportTaskRepository.FindAsync(id, cancellationToken: cancellationToken);
        if (entity == null || entity.UserId != userId)
        {
            throw new BusinessException($"导出任务不存在: {id}");
        }

        return entity;
    }

    private Guid GetCurrentUserId()
    {
        if (_currentUser.Id is null)
        {
            throw new BusinessException("未登录或登录已失效");
        }

        return _currentUser.Id.Value;
    }

    private static ExportTaskDto ToDto(ExportTask entity) => new()
    {
        Id = entity.Id,
        ExportType = entity.ExportType,
        Parameters = entity.Parameters,
        FileName = entity.FileName,
        FileUrl = entity.FileUrl,
        Status = entity.Status,
        ErrorMessage = entity.ErrorMessage,
        CreationTime = entity.CreationTime,
        CompletedTime = entity.CompletedTime
    };
}

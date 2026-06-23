using System.Text.Json;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Contracts.Ticket;
using BeaverX.Admin.Application.Contracts.Ticket.Dtos;
using BeaverX.Admin.Application.Rbac;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Ticket;
using BeaverX.Admin.Domain.Shared.Ticket;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using BeaverX.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Ticket;

public class WorkTicketAppService : IWorkTicketAppService, IScopedDependency
{
    public const int MaxImages = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IRepository<WorkTicket> _workTicketRepository;
    private readonly IRepository<User> _userRepository;
    private readonly ICurrentUser _currentUser;

    public WorkTicketAppService(
        IRepository<WorkTicket> workTicketRepository,
        IRepository<User> userRepository,
        ICurrentUser currentUser)
    {
        _workTicketRepository = workTicketRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public Task<PagedResultDto<WorkTicketDto>> GetListAsync(
        WorkTicketQueryDto input,
        CancellationToken cancellationToken = default)
        => QueryPageAsync(input, null, cancellationToken);

    public Task<PagedResultDto<WorkTicketDto>> GetProcessListAsync(
        WorkTicketQueryDto input,
        CancellationToken cancellationToken = default)
        => QueryPageAsync(
            input,
            query => query.Where(x =>
                x.Status == WorkTicketStatus.Pending ||
                x.Status == WorkTicketStatus.Processing),
            cancellationToken);

    public async Task<WorkTicketDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        var userMap = await LoadUserMapAsync([entity.UserId, entity.HandlerUserId ?? 0], cancellationToken);
        return ToDto(entity, userMap);
    }

    public async Task<WorkTicketDto> CreateAsync(
        CreateWorkTicketDto input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            throw new BusinessException("工单标题不能为空");
        }

        if (string.IsNullOrWhiteSpace(input.Content))
        {
            throw new BusinessException("工单内容不能为空");
        }

        var images = ValidateImages(input.Images);
        var userId = GetCurrentUserId();

        var entity = new WorkTicket
        {
            TicketNo = GenerateTicketNo(),
            Title = input.Title.Trim(),
            Content = input.Content.Trim(),
            UserId = userId,
            ImagesJson = SerializeImages(images)
        };

        await _workTicketRepository.InsertAsync(entity, cancellationToken: cancellationToken);
        var userMap = await LoadUserMapAsync([userId], cancellationToken);
        return ToDto(entity, userMap);
    }

    public async Task<WorkTicketDto> UpdateAsync(
        long id,
        UpdateWorkTicketDto input,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);

        if (entity.Status is WorkTicketStatus.Resolved or WorkTicketStatus.Closed)
        {
            throw new BusinessException("已完结的工单不能修改");
        }

        if (input.Title != null)
        {
            if (string.IsNullOrWhiteSpace(input.Title))
            {
                throw new BusinessException("工单标题不能为空");
            }

            entity.Title = input.Title.Trim();
        }

        if (input.Content != null)
        {
            if (string.IsNullOrWhiteSpace(input.Content))
            {
                throw new BusinessException("工单内容不能为空");
            }

            entity.Content = input.Content.Trim();
        }

        if (input.Images != null)
        {
            var images = ValidateImages(input.Images);
            entity.ImagesJson = SerializeImages(images);
        }

        await _workTicketRepository.UpdateAsync(entity, cancellationToken: cancellationToken);
        var userMap = await LoadUserMapAsync([entity.UserId, entity.HandlerUserId ?? 0], cancellationToken);
        return ToDto(entity, userMap);
    }

    public async Task<WorkTicketDto> ProcessAsync(
        long id,
        ProcessWorkTicketDto input,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);

        if (entity.Status is WorkTicketStatus.Resolved or WorkTicketStatus.Closed)
        {
            throw new BusinessException("工单已完结，不能重复处理");
        }

        if (input.Status is WorkTicketStatus.Pending)
        {
            throw new BusinessException("处理状态不能为待处理");
        }

        if (string.IsNullOrWhiteSpace(input.ProcessResult))
        {
            throw new BusinessException("处理结果不能为空");
        }

        var resultImages = ValidateImages(input.ProcessResultImages);
        var handlerUserId = GetCurrentUserId();

        entity.Status = input.Status;
        entity.ProcessResult = input.ProcessResult.Trim();
        entity.ProcessResultImagesJson = SerializeImages(resultImages);
        entity.HandlerUserId = handlerUserId;
        entity.ProcessedTime = DateTime.UtcNow;

        await _workTicketRepository.UpdateAsync(entity, cancellationToken: cancellationToken);
        var userMap = await LoadUserMapAsync([entity.UserId, handlerUserId], cancellationToken);
        return ToDto(entity, userMap);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        await FindAsync(id, cancellationToken);
        await _workTicketRepository.DeleteAsync(id, cancellationToken: cancellationToken);
    }

    private async Task<PagedResultDto<WorkTicketDto>> QueryPageAsync(
        WorkTicketQueryDto input,
        Func<IQueryable<WorkTicket>, IQueryable<WorkTicket>>? filter,
        CancellationToken cancellationToken)
    {
        var query = _workTicketRepository.GetQueryable().AsQueryable();
        if (filter != null)
        {
            query = filter(query);
        }

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            query = query.Where(x =>
                x.TicketNo.Contains(keyword) ||
                x.Title.Contains(keyword) ||
                x.Content.Contains(keyword) ||
                (x.ProcessResult != null && x.ProcessResult.Contains(keyword)));
        }

        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var (skip, take) = RbacQueryHelper.GetPaging(input.Page, input.PageSize);
        var items = await query
            .OrderByDescending(x => x.CreationTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var userIds = items
            .SelectMany(x => new long?[] { x.UserId, x.HandlerUserId })
            .Where(id => id.HasValue && id.Value > 0)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var userMap = await LoadUserMapAsync(userIds, cancellationToken);

        return new PagedResultDto<WorkTicketDto>
        {
            Total = total,
            Items = items.Select(entity => ToDto(entity, userMap)).ToList()
        };
    }

    private async Task<WorkTicket> FindAsync(long id, CancellationToken cancellationToken)
    {
        var entity = await _workTicketRepository.FindAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            throw new BusinessException($"工单不存在: {id}");
        }

        return entity;
    }

    private long GetCurrentUserId()
    {
        if (!_currentUser.Id.HasValue || _currentUser.Id.Value <= 0)
        {
            throw new BusinessException("未登录或用户信息无效");
        }

        return _currentUser.Id.Value;
    }

    private async Task<Dictionary<long, string>> LoadUserMapAsync(
        IEnumerable<long> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        return await _userRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.NickName, x.UserName })
            .ToDictionaryAsync(
                x => x.Id,
                x => x.NickName ?? x.UserName,
                cancellationToken);
    }

    private static string GenerateTicketNo() =>
        $"WT{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";

    private static List<WorkTicketImageDto> ValidateImages(List<WorkTicketImageDto>? images)
    {
        if (images == null || images.Count == 0)
        {
            return [];
        }

        if (images.Count > MaxImages)
        {
            throw new BusinessException($"最多上传 {MaxImages} 张图片");
        }

        var result = new List<WorkTicketImageDto>();
        foreach (var image in images)
        {
            if (string.IsNullOrWhiteSpace(image.ObjectKey) ||
                string.IsNullOrWhiteSpace(image.ProxyUrl) ||
                string.IsNullOrWhiteSpace(image.FileName))
            {
                throw new BusinessException("图片信息不完整");
            }

            result.Add(new WorkTicketImageDto
            {
                ObjectKey = image.ObjectKey.Trim(),
                ProxyUrl = image.ProxyUrl.Trim(),
                FileName = image.FileName.Trim()
            });
        }

        return result;
    }

    private static string? SerializeImages(List<WorkTicketImageDto> images) =>
        images.Count == 0 ? null : JsonSerializer.Serialize(images, JsonOptions);

    private static List<WorkTicketImageDto> DeserializeImages(string? imagesJson)
    {
        if (string.IsNullOrWhiteSpace(imagesJson))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<WorkTicketImageDto>>(imagesJson, JsonOptions) ?? [];
    }

    private static WorkTicketDto ToDto(WorkTicket entity, Dictionary<long, string> userMap)
    {
        userMap.TryGetValue(entity.UserId, out var creatorName);
        string? handlerName = null;
        if (entity.HandlerUserId.HasValue)
        {
            userMap.TryGetValue(entity.HandlerUserId.Value, out handlerName);
        }

        return new WorkTicketDto
        {
            Id = entity.Id,
            TicketNo = entity.TicketNo,
            Title = entity.Title,
            Content = entity.Content,
            Status = entity.Status,
            UserId = entity.UserId,
            CreatorName = creatorName,
            Images = DeserializeImages(entity.ImagesJson),
            ProcessResult = entity.ProcessResult,
            ProcessResultImages = DeserializeImages(entity.ProcessResultImagesJson),
            HandlerUserId = entity.HandlerUserId,
            HandlerName = handlerName,
            ProcessedTime = entity.ProcessedTime,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime
        };
    }
}

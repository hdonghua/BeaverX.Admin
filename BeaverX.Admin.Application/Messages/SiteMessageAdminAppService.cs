using BeaverX.Admin.Application.Contracts.Messages;
using BeaverX.Admin.Application.Contracts.Messages.Dtos;
using BeaverX.Admin.Application.Contracts.Messaging;
using BeaverX.Admin.Application.Contracts.Messaging.Dtos;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Messaging;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Messages;

public class SiteMessageAdminAppService : ISiteMessageAdminAppService, IScopedDependency
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "message", "notice", "todo"
    };

    private readonly IMessageSender _messageSender;
    private readonly IRepository<User> _userRepository;

    public SiteMessageAdminAppService(
        IMessageSender messageSender,
        IRepository<User> userRepository)
    {
        _messageSender = messageSender;
        _userRepository = userRepository;
    }

    public async Task<SendSiteMessageResultDto> SendAsync(
        SendSiteMessageDto input,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(input);

        var userIds = await ResolveTargetUserIdsAsync(input, cancellationToken);
        if (userIds.Count == 0)
        {
            throw new BusinessException("没有可发送的目标用户");
        }

        var type = input.Type.Trim().ToLowerInvariant();
        var successCount = 0;
        var failCount = 0;

        foreach (var userId in userIds)
        {
            var result = await _messageSender.SendAsync(
                new SendMessageRequest
                {
                    UserId = userId,
                    Title = input.Title.Trim(),
                    SubTitle = string.IsNullOrWhiteSpace(input.SubTitle) ? null : input.SubTitle.Trim(),
                    Content = input.Content.Trim(),
                    Type = type,
                    MessageType = input.MessageType,
                    Channels = [MessageChannels.Site]
                },
                cancellationToken);

            if (result.ChannelResults.TryGetValue(MessageChannels.Site, out var channelResult) &&
                channelResult.Success)
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        return new SendSiteMessageResultDto
        {
            SuccessCount = successCount,
            FailCount = failCount
        };
    }

    private static void ValidateInput(SendSiteMessageDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            throw new BusinessException("消息标题不能为空");
        }

        if (string.IsNullOrWhiteSpace(input.Content))
        {
            throw new BusinessException("消息内容不能为空");
        }

        if (!input.SendToAll && input.UserId is not > 0)
        {
            throw new BusinessException("请选择接收用户或勾选发送给全部用户");
        }

        var type = input.Type?.Trim();
        if (string.IsNullOrWhiteSpace(type) || !AllowedTypes.Contains(type))
        {
            throw new BusinessException("消息分类无效，可选 message / notice / todo");
        }
    }

    private async Task<List<long>> ResolveTargetUserIdsAsync(
        SendSiteMessageDto input,
        CancellationToken cancellationToken)
    {
        if (input.SendToAll)
        {
            return await _userRepository.GetQueryable()
                .Where(x => x.IsEnabled)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        var userId = input.UserId!.Value;
        var exists = await _userRepository.AnyAsync(
            x => x.Id == userId && x.IsEnabled,
            cancellationToken);

        if (!exists)
        {
            throw new BusinessException("目标用户不存在或已被禁用");
        }

        return [userId];
    }
}

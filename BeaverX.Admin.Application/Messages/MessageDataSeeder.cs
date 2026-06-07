using BeaverX.Admin.Domain.Messages;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Messages;

public class MessageDataSeeder : IScopedDependency
{
    private readonly IRepository<UserMessage> _messageRepository;
    private readonly IRepository<User> _userRepository;
    private readonly ILogger<MessageDataSeeder> _logger;

    public MessageDataSeeder(
        IRepository<UserMessage> messageRepository,
        IRepository<User> userRepository,
        ILogger<MessageDataSeeder> logger)
    {
        _messageRepository = messageRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _messageRepository.AnyAsync(_ => true, cancellationToken))
        {
            return;
        }

        var adminUser = await _userRepository.FindAsync(
            x => x.UserName == "admin",
            cancellationToken);
        if (adminUser == null)
        {
            return;
        }

        _logger.LogInformation("Seeding demo user messages for admin...");

        var now = DateTime.UtcNow;
        var messages = new List<UserMessage>
        {
            new()
            {
                UserId = adminUser.Id,
                Type = "message",
                Title = "系统管理员",
                SubTitle = "的私信",
                Content = "欢迎使用 BeaverX Admin",
                IsRead = false,
                CreationTime = now.AddMinutes(-30)
            },
            new()
            {
                UserId = adminUser.Id,
                Type = "message",
                Title = "运维通知",
                SubTitle = "的回复",
                Content = "系统已完成初始化配置",
                IsRead = false,
                CreationTime = now.AddHours(-2)
            },
            new()
            {
                UserId = adminUser.Id,
                Type = "message",
                Title = "安全提醒",
                SubTitle = "",
                Content = "请及时修改默认密码",
                IsRead = true,
                CreationTime = now.AddDays(-1)
            },
            new()
            {
                UserId = adminUser.Id,
                Type = "notice",
                Title = "续费通知",
                SubTitle = "",
                Content = "您的产品使用期限即将截止，如需继续使用产品请前往续费",
                MessageType = 3,
                IsRead = false,
                CreationTime = now.AddHours(-1)
            },
            new()
            {
                UserId = adminUser.Id,
                Type = "notice",
                Title = "规则开通成功",
                SubTitle = "",
                Content = "内容屏蔽规则已开通成功并生效",
                MessageType = 1,
                IsRead = true,
                CreationTime = now.AddDays(-2)
            },
            new()
            {
                UserId = adminUser.Id,
                Type = "todo",
                Title = "质检队列变更",
                SubTitle = "",
                Content = "内容质检队列已变更，请重新确认处理规则",
                MessageType = 0,
                IsRead = false,
                CreationTime = now.AddMinutes(-90)
            }
        };

        await _messageRepository.InsertManyAsync(messages, cancellationToken: cancellationToken);
        _logger.LogInformation("Demo user messages seeded.");
    }
}

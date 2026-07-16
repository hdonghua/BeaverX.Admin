using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Admin.Domain.Messages;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Core.Dependency;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Messages;

public class MessageDataSeeder : IScopedDependency, IDataSeeder
{
    private readonly ISugarRepository<UserMessage> _messageRepository;
    private readonly ISugarRepository<User> _userRepository;
    private readonly ILogger<MessageDataSeeder> _logger;

    public MessageDataSeeder(
      ISugarRepository<UserMessage> messageRepository,
      ISugarRepository<User> userRepository,
      ILogger<MessageDataSeeder> logger)
    {
        _messageRepository = messageRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var adminUser = await _userRepository.GetSugarQueryable()
          .FirstAsync(x => x.UserName == "admin", cancellationToken);

        if (adminUser == null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        await EnsureMessageAsync(
          adminUser.Id,
          "续费通知",
          () => new UserMessage
          {
              UserId = adminUser.Id,
              Type = "notice",
              Title = "续费通知",
              SubTitle = "",
              Content = "您的产品使用期限即将截止，如需继续使用产品请前往续费",
              MessageType = 3,
              IsRead = false,
              CreationTime = now.AddHours(-1),
          },
          cancellationToken);

        await EnsureMessageAsync(
          adminUser.Id,
          "规则开通成功",
          () => new UserMessage
          {
              UserId = adminUser.Id,
              Type = "notice",
              Title = "规则开通成功",
              SubTitle = "",
              Content = "内容屏蔽规则已开通成功并生效",
              MessageType = 1,
              IsRead = true,
              CreationTime = now.AddDays(-2),
          },
          cancellationToken);

        await EnsureMessageAsync(
          adminUser.Id,
          "质检队列变更",
          () => new UserMessage
          {
              UserId = adminUser.Id,
              Type = "notice",
              Title = "质检队列变更",
              SubTitle = "",
              Content = "内容质检队列已变更，请重新确认处理规则",
              MessageType = 0,
              IsRead = false,
              CreationTime = now.AddMinutes(-90),
          },
          cancellationToken);
    }

    private async Task EnsureMessageAsync(
      long userId,
      string title,
      Func<UserMessage> factory,
      CancellationToken cancellationToken)
    {
        if (await _messageRepository.AnyAsync(
            x => x.UserId == userId && x.Title == title,
            cancellationToken))
        {
            return;
        }

        _logger.LogInformation("Seeding demo message {Title}...", title);
        await _messageRepository.InsertAsync(factory(), cancellationToken: cancellationToken);
    }
}

namespace BeaverX.Admin.Application.Contracts.Messaging.Dtos;

public class SendMessageRequest
{
    public long UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? SubTitle { get; set; }
    public string? Avatar { get; set; }

    /// <summary>站内信分类：message / notice / todo</summary>
    public string Type { get; set; } = "message";

    /// <summary>前端展示用消息子类型（如 notice 的续费/开通等）</summary>
    public int? MessageType { get; set; }

    /// <summary>
    /// 指定发送渠道；为空时发送到全部已注册渠道。
    /// 渠道常量见 MessageChannels（site / dingtalk / wecom）。
    /// </summary>
    public IReadOnlyList<string>? Channels { get; set; }
}

public class MessageDeliveryContext
{
    public SendMessageRequest Request { get; init; } = null!;
}

public class ChannelSendResult
{
    public bool Success { get; set; }
    public string? ExternalId { get; set; }
    public string? ErrorMessage { get; set; }

    public static ChannelSendResult Ok(string? externalId = null) => new()
    {
        Success = true,
        ExternalId = externalId
    };

    public static ChannelSendResult Fail(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}

public class MessageSendResult
{
    public IReadOnlyDictionary<string, ChannelSendResult> ChannelResults { get; set; }
        = new Dictionary<string, ChannelSendResult>();

    public bool IsAllSuccess => ChannelResults.Values.All(x => x.Success);
}

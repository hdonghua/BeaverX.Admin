namespace BeaverX.Admin.Application.Contracts.Messages.Dtos;

public class SendSiteMessageDto
{
    /// <summary>接收用户；SendToAll=true 时可空</summary>
    public long? UserId { get; set; }

    /// <summary>发送给全部启用用户</summary>
    public bool SendToAll { get; set; }

    public string Title { get; set; } = null!;
    public string? SubTitle { get; set; }
    public string Content { get; set; } = null!;

    /// <summary>message / notice / todo</summary>
    public string Type { get; set; } = "notice";

    public int? MessageType { get; set; }
}

public class SendSiteMessageResultDto
{
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
}

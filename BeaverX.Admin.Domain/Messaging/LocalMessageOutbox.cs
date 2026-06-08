using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Messaging;

/// <summary>
/// 通用本地消息表：与 CAP 消息表配合，为各类异步业务提供发布/消费幂等。
/// </summary>
public class LocalMessageOutbox : Entity
{
    /// <summary>消息类型，如 export.task.execute</summary>
    public string MessageType { get; set; } = null!;

    /// <summary>业务主键（字符串），如导出任务 Id</summary>
    public string BusinessKey { get; set; } = null!;

    /// <summary>全局幂等键，默认 {MessageType}:{BusinessKey}</summary>
    public string IdempotencyKey { get; set; } = null!;

    /// <summary>可选业务快照（JSON）</summary>
    public string? Payload { get; set; }

    public bool IsPublished { get; set; }

    public bool IsConsumed { get; set; }

    public string? CapConsumeMessageId { get; set; }

    public DateTime? PublishedTime { get; set; }

    public DateTime? ConsumedTime { get; set; }
}

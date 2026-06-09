using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Messaging;

/// <summary>
/// CAP 消息消费记录：同一 cap_message_id 只成功消费一次，业务幂等由业务自行处理。
/// </summary>
public class LocalMessageOutbox : Entity
{
    public string CapMessageId { get; set; } = null!;

    public DateTime ConsumedTime { get; set; }
}

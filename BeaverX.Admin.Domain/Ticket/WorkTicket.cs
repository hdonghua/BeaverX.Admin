using BeaverX.Admin.Domain.Shared.Ticket;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Ticket;

[SugarTable("biz_work_tickets")]
public class WorkTicket : FullAuditedEntity
{
    public string TicketNo { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public WorkTicketStatus Status { get; set; } = WorkTicketStatus.Pending;
    public long UserId { get; set; }
    public string? ImagesJson { get; set; }
    public string? ProcessResult { get; set; }
    public string? ProcessResultImagesJson { get; set; }
    public long? HandlerUserId { get; set; }
    public DateTime? ProcessedTime { get; set; }
}

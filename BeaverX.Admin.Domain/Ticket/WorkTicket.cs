using BeaverX.Admin.Domain.Shared.Ticket;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Domain.Entities;

namespace BeaverX.Admin.Domain.Ticket;

public class WorkTicket : FullAuditedEntity<Guid>
{
    public string TicketNo { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public WorkTicketStatus Status { get; set; } = WorkTicketStatus.Pending;
    public Guid UserId { get; set; }
    public string? ImagesJson { get; set; }
    public string? ProcessResult { get; set; }
    public string? ProcessResultImagesJson { get; set; }
    public Guid? HandlerUserId { get; set; }
    public DateTime? ProcessedTime { get; set; }
}

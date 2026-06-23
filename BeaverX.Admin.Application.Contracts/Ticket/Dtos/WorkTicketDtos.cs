using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Shared.Ticket;

namespace BeaverX.Admin.Application.Contracts.Ticket.Dtos;

public class WorkTicketImageDto
{
    public string ObjectKey { get; set; } = null!;
    public string ProxyUrl { get; set; } = null!;
    public string FileName { get; set; } = null!;
}

public class WorkTicketDto
{
    public long Id { get; set; }
    public string TicketNo { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public WorkTicketStatus Status { get; set; }
    public long UserId { get; set; }
    public string? CreatorName { get; set; }
    public List<WorkTicketImageDto> Images { get; set; } = [];
    public string? ProcessResult { get; set; }
    public List<WorkTicketImageDto> ProcessResultImages { get; set; } = [];
    public long? HandlerUserId { get; set; }
    public string? HandlerName { get; set; }
    public DateTime? ProcessedTime { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
}

public class WorkTicketQueryDto : PagedQueryDto
{
    public string? Keyword { get; set; }
    public WorkTicketStatus? Status { get; set; }
}

public class CreateWorkTicketDto
{
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public List<WorkTicketImageDto>? Images { get; set; }
}

public class UpdateWorkTicketDto
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public List<WorkTicketImageDto>? Images { get; set; }
}

public class ProcessWorkTicketDto
{
    public WorkTicketStatus Status { get; set; }
    public string ProcessResult { get; set; } = null!;
    public List<WorkTicketImageDto>? ProcessResultImages { get; set; }
}

using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Contracts.Ticket;
using BeaverX.Admin.Application.Contracts.Ticket.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class WorkTicketController : AdminControllerBase
{
    private readonly IWorkTicketAppService _workTicketAppService;

    public WorkTicketController(IWorkTicketAppService workTicketAppService)
    {
        _workTicketAppService = workTicketAppService;
    }

    [RequirePermission(RbacPermissionCodes.Ticket.Work.List)]
    [HttpGet("list")]
    public Task<PagedResultDto<WorkTicketDto>> GetListAsync(
        [FromQuery] WorkTicketQueryDto input,
        CancellationToken cancellationToken)
        => _workTicketAppService.GetListAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Ticket.Work.List)]
    [HttpGet("{id:guid}")]
    public Task<WorkTicketDto> GetAsync(Guid id, CancellationToken cancellationToken)
        => _workTicketAppService.GetAsync(id, cancellationToken: cancellationToken);

    [RequirePermission(RbacPermissionCodes.Ticket.Work.Create)]
    [HttpPost]
    public Task<WorkTicketDto> CreateAsync(
        [FromBody] CreateWorkTicketDto input,
        CancellationToken cancellationToken)
        => _workTicketAppService.CreateAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Ticket.Work.Update)]
    [HttpPut("{id:guid}")]
    public Task<WorkTicketDto> UpdateAsync(
        Guid id,
        [FromBody] UpdateWorkTicketDto input,
        CancellationToken cancellationToken)
        => _workTicketAppService.UpdateAsync(id, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Ticket.Work.Delete)]
    [HttpDelete("{id:guid}")]
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        => _workTicketAppService.DeleteAsync(id, cancellationToken: cancellationToken);

    [RequirePermission(RbacPermissionCodes.Ticket.Work.Process)]
    [HttpGet("process-list")]
    public Task<PagedResultDto<WorkTicketDto>> GetProcessListAsync(
        [FromQuery] WorkTicketQueryDto input,
        CancellationToken cancellationToken)
        => _workTicketAppService.GetProcessListAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Ticket.Work.Process)]
    [HttpPost("{id:guid}/process")]
    public Task<WorkTicketDto> ProcessAsync(
        Guid id,
        [FromBody] ProcessWorkTicketDto input,
        CancellationToken cancellationToken)
        => _workTicketAppService.ProcessAsync(id, input, cancellationToken);
}

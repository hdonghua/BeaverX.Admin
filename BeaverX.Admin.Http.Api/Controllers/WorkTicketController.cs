using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Contracts.Ticket;
using BeaverX.Admin.Application.Contracts.Ticket.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using BeaverX.WebMvc.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class WorkTicketController : BeaverXControllerBase
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
    [HttpGet("{id:long}")]
    public Task<WorkTicketDto> GetAsync(long id, CancellationToken cancellationToken)
        => _workTicketAppService.GetAsync(id, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Ticket.Work.Create)]
    [HttpPost]
    public Task<WorkTicketDto> CreateAsync(
        [FromBody] CreateWorkTicketDto input,
        CancellationToken cancellationToken)
        => _workTicketAppService.CreateAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Ticket.Work.Update)]
    [HttpPut("{id:long}")]
    public Task<WorkTicketDto> UpdateAsync(
        long id,
        [FromBody] UpdateWorkTicketDto input,
        CancellationToken cancellationToken)
        => _workTicketAppService.UpdateAsync(id, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Ticket.Work.Delete)]
    [HttpDelete("{id:long}")]
    public Task DeleteAsync(long id, CancellationToken cancellationToken)
        => _workTicketAppService.DeleteAsync(id, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Ticket.Work.Process)]
    [HttpGet("process-list")]
    public Task<PagedResultDto<WorkTicketDto>> GetProcessListAsync(
        [FromQuery] WorkTicketQueryDto input,
        CancellationToken cancellationToken)
        => _workTicketAppService.GetProcessListAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Ticket.Work.Process)]
    [HttpPost("{id:long}/process")]
    public Task<WorkTicketDto> ProcessAsync(
        long id,
        [FromBody] ProcessWorkTicketDto input,
        CancellationToken cancellationToken)
        => _workTicketAppService.ProcessAsync(id, input, cancellationToken);
}

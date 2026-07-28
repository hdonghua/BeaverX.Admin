using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Contracts.Scheduling;
using BeaverX.Admin.Application.Contracts.Scheduling.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class ScheduledJobController : AdminControllerBase
{
    private readonly IScheduledJobAppService _scheduledJobAppService;

    public ScheduledJobController(IScheduledJobAppService scheduledJobAppService)
    {
        _scheduledJobAppService = scheduledJobAppService;
    }

    [RequirePermission(RbacPermissionCodes.System.Job.List)]
    [HttpGet("list")]
    public Task<PagedResultDto<ScheduledJobDto>> GetListAsync(
        [FromQuery] ScheduledJobQueryDto input,
        CancellationToken cancellationToken)
        => _scheduledJobAppService.GetListAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Job.List)]
    [HttpGet("{id:guid}")]
    public Task<ScheduledJobDto> GetAsync(Guid id, CancellationToken cancellationToken)
        => _scheduledJobAppService.GetAsync(id, cancellationToken: cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Job.List)]
    [HttpGet("{id:guid}/logs")]
    public Task<PagedResultDto<ScheduledJobLogDto>> GetLogsAsync(
        Guid id,
        [FromQuery] ScheduledJobLogQueryDto input,
        CancellationToken cancellationToken)
        => _scheduledJobAppService.GetLogsAsync(id, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Job.Create)]
    [HttpPost]
    public Task<ScheduledJobDto> CreateAsync(
        [FromBody] CreateScheduledJobDto input,
        CancellationToken cancellationToken)
        => _scheduledJobAppService.CreateAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Job.Update)]
    [HttpPut("{id:guid}")]
    public Task<ScheduledJobDto> UpdateAsync(
        Guid id,
        [FromBody] UpdateScheduledJobDto input,
        CancellationToken cancellationToken)
        => _scheduledJobAppService.UpdateAsync(id, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Job.Delete)]
    [HttpDelete("{id:guid}")]
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        => _scheduledJobAppService.DeleteAsync(id, cancellationToken: cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Job.Trigger)]
    [HttpPost("{id:guid}/trigger")]
    public Task TriggerAsync(Guid id, CancellationToken cancellationToken)
        => _scheduledJobAppService.TriggerAsync(id, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Job.List)]
    [HttpPost("validate-cron")]
    public ValidateCronResultDto ValidateCron([FromBody] ValidateCronDto input)
        => _scheduledJobAppService.ValidateCron(input);
}

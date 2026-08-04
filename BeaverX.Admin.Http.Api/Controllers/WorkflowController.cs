using BeaverX.Admin.Application.Contracts.Oa;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

[Route("api/workflow")]
public class WorkflowController : AdminControllerBase
{
    private readonly IOaWorkflowAppService _service;
    public WorkflowController(IOaWorkflowAppService service) => _service = service;

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpGet("getEnabledFlowGroupWithDef")]
    public Task<List<OaFlowGroupDto>> GetEnabledAsync([FromQuery] OaFlowGroupQuery input, CancellationToken cancellationToken) => _service.GetGroupsWithDefinitionsAsync(input, true, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpGet("getFlowFormWidget")]
    public Task<List<OaFlowFormFieldDto>> GetWidgetsAsync([FromQuery] Guid defId, CancellationToken cancellationToken) => _service.GetFlowFormWidgetsAsync(defId, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpPost("lanunch")]
    public Task LaunchAsync([FromBody] OaLaunchRequest input, CancellationToken cancellationToken) => _service.LaunchAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpGet("viewProcessChart")]
    public Task<List<OaFlowChartNodeDto>> ViewChartAsync([FromQuery] Guid defId, CancellationToken cancellationToken) => _service.ViewProcessChartAsync(defId, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpGet("queryPendingMyApprovalFlowInsts")]
    public Task<PagedResultDto<OaFlowInstanceListDto>> PendingAsync([FromQuery] OaFlowInstanceQuery input, CancellationToken cancellationToken) => _service.QueryPendingAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpGet("queryMyApplyFlowInstances")]
    public Task<PagedResultDto<OaFlowInstanceListDto>> MineAsync([FromQuery] OaFlowInstanceQuery input, CancellationToken cancellationToken) => _service.QueryMyApplyAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpGet("queryCcMimeFlowInstanceAsync")]
    public Task<PagedResultDto<OaFlowInstanceListDto>> CcAsync([FromQuery] OaFlowInstanceQuery input, CancellationToken cancellationToken) => _service.QueryCcAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpGet("queryMimeAuditFlowInstance")]
    public Task<PagedResultDto<OaFlowInstanceListDto>> AuditedAsync([FromQuery] OaFlowInstanceQuery input, CancellationToken cancellationToken) => _service.QueryAuditedAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpGet("getFlowInstanceDetails")]
    public Task<OaFlowInstanceDetailsDto> DetailsAsync([FromQuery] Guid instanceId, CancellationToken cancellationToken) => _service.GetDetailsAsync(instanceId, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpGet("getFlowInstanceSummary")]
    public Task<OaFlowInstanceListDto> SummaryAsync([FromQuery] Guid instanceId, CancellationToken cancellationToken) => _service.GetInstanceSummaryAsync(instanceId, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpPost("formModify")]
    public Task FormModifyAsync([FromBody] OaFormModifyRequest input, CancellationToken cancellationToken) => _service.FormModifyAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpGet("hasFormEditRecord")]
    public Task<bool> HasFormEditRecordAsync([FromQuery] Guid flowInstId, CancellationToken cancellationToken) => _service.HasFormEditRecordAsync(flowInstId, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpGet("listFormEditRecords")]
    public Task<List<OaFormEditRecordDto>> GetFormEditRecordsAsync([FromQuery] Guid flowInstId, CancellationToken cancellationToken) => _service.GetFormEditRecordsAsync(flowInstId, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.WorkflowData)]
    [HttpPost("urge")]
    public Task UrgeAsync([FromBody] OaFlowInstanceIdRequest input, CancellationToken cancellationToken) => _service.UrgeAsync(input.FlowInstId, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpPost("comment")]
    public Task CommentAsync([FromBody] OaCommentRequest input, CancellationToken cancellationToken) => _service.CommentAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpPost("approve")]
    public Task ApproveAsync([FromBody] OaTaskActionRequest input, CancellationToken cancellationToken) => _service.ApproveAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpPost("assign")]
    public Task AssignAsync([FromBody] OaTaskActionRequest input, CancellationToken cancellationToken) => _service.AssignAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpPost("addSign")]
    public Task AddSignAsync([FromBody] OaTaskActionRequest input, CancellationToken cancellationToken) => _service.AddSignAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpPost("delSign")]
    public Task DelSignAsync([FromBody] OaTaskActionRequest input, CancellationToken cancellationToken) => _service.DelSignAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpPost("jump")]
    public Task JumpAsync([FromBody] OaTaskActionRequest input, CancellationToken cancellationToken) => _service.JumpAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.Approval)]
    [HttpPost("cancel")]
    public Task CancelAsync([FromBody] OaTaskActionRequest input, CancellationToken cancellationToken) => _service.CancelAsync(input, cancellationToken);
}

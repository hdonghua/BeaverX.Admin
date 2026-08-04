using BeaverX.Admin.Application.Contracts.Oa;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

[Route("api/approveManagement")]
public class ApproveManagementController : AdminControllerBase
{
    private readonly IOaProcessDefinitionAppService _processDefinitionService;
    private readonly IOaWorkflowDataAppService _workflowDataService;

    public ApproveManagementController(
        IOaProcessDefinitionAppService processDefinitionService,
        IOaWorkflowDataAppService workflowDataService)
    {
        _processDefinitionService = processDefinitionService;
        _workflowDataService = workflowDataService;
    }

    [RequirePermission(RbacPermissionCodes.Oa.WorkflowManage)]
    [HttpGet("getFlowGroupWithDef")]
    public Task<List<OaFlowGroupDto>> GetFlowGroupWithDefAsync([FromQuery] OaFlowGroupQuery input, CancellationToken cancellationToken) =>
        _processDefinitionService.GetGroupsWithDefinitionsAsync(input, cancellationToken: cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.WorkflowManage)]
    [HttpPost("addProcessGroup")]
    public Task<OaProcessGroupDto> AddProcessGroupAsync([FromBody] OaAddProcessGroupRequest input, CancellationToken cancellationToken) =>
        _processDefinitionService.AddGroupAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.WorkflowManage)]
    [HttpPost("saveOrUpdateGroup")]
    public Task<OaProcessGroupDto> UpdateProcessGroupAsync([FromBody] OaUpdateProcessGroupRequest input, CancellationToken cancellationToken) =>
        _processDefinitionService.UpdateGroupAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.WorkflowManage)]
    [HttpPost("deleteGroup")]
    public Task DeleteProcessGroupAsync([FromBody] OaIdRequest input, CancellationToken cancellationToken) =>
        _processDefinitionService.DeleteGroupAsync(input.Id, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.WorkflowManage)]
    [HttpGet("getFlowGroups")]
    public Task<List<OaProcessGroupDto>> GetFlowGroupsAsync(CancellationToken cancellationToken) => _processDefinitionService.GetGroupsAsync(cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.WorkflowManage)]
    [HttpGet("getSvgIcons")]
    public List<string> GetSvgIcons() =>
    [
        "approval.svg", "bank-card.svg", "bell.svg", "box.svg", "calendar.svg", "car.svg", "cart.svg",
        "cash.svg", "checklist.svg", "clock.svg", "coin.svg", "contract.svg", "dimission.svg", "exchange.svg",
        "lightning.svg", "location.svg", "male.svg", "manager.svg", "offboarding.svg", "onboarding.svg",
        "plane.svg", "presentation.svg", "propotion.svg", "regular.svg", "relation.svg", "ticket.svg",
        "toolbox.svg", "transfer.svg", "wallet.svg"
    ];

    [RequirePermission(RbacPermissionCodes.Oa.WorkflowManage)]
    [HttpPost("addProcess")]
    public Task AddProcessAsync([FromBody] OaAddProcessRequest input, CancellationToken cancellationToken) => _processDefinitionService.AddProcessAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.WorkflowManage)]
    [HttpPost("updateProcess")]
    public Task UpdateProcessAsync([FromBody] OaAddProcessRequest input, CancellationToken cancellationToken) => _processDefinitionService.UpdateProcessAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.WorkflowManage)]
    [HttpPost("removeById")]
    public Task DeleteProcessAsync([FromBody] OaFlowDefinitionIdRequest input, CancellationToken cancellationToken) =>
        _processDefinitionService.DeleteProcessAsync(input.FlowDefId, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.WorkflowManage)]
    [HttpPost("freezeById")]
    public Task FreezeProcessAsync([FromBody] OaFlowDefinitionIdRequest input, CancellationToken cancellationToken) =>
        _processDefinitionService.SetProcessEnabledAsync(input.FlowDefId, false, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.WorkflowManage)]
    [HttpPost("enableById")]
    public Task EnableProcessAsync([FromBody] OaFlowDefinitionIdRequest input, CancellationToken cancellationToken) =>
        _processDefinitionService.SetProcessEnabledAsync(input.FlowDefId, true, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.WorkflowManage)]
    [HttpPost("copy")]
    public Task<OaFlowDefinitionDto> CopyProcessAsync([FromBody] OaCopyProcessRequest input, CancellationToken cancellationToken) =>
        _processDefinitionService.CopyProcessAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.WorkflowManage)]
    [HttpGet("getProcessEditData")]
    public Task<OaProcessEditDto> GetProcessEditDataAsync([FromQuery] Guid defId, CancellationToken cancellationToken) => _processDefinitionService.GetProcessEditDataAsync(defId, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.WorkflowData)]
    [HttpGet("queryFlowInstsData")]
    public Task<PagedResultDto<OaFlowInstanceListDto>> QueryFlowInstsDataAsync([FromQuery] OaFlowInstanceQuery input, CancellationToken cancellationToken) => _workflowDataService.QueryInstancesAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.Oa.WorkflowData)]
    [HttpPost("transfer")]
    public Task TransferAsync([FromBody] OaTransferRequest input, CancellationToken cancellationToken) => _workflowDataService.TransferAsync(input, cancellationToken);
}

using BeaverX.Admin.Application.Contracts.Oa;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

[Authorize]
public class OaOrganizationController : AdminControllerBase
{
    private readonly IOaOrganizationAppService _service;

    public OaOrganizationController(IOaOrganizationAppService service) => _service = service;

    [HttpGet("options")]
    public Task<OaOrganizationOptionsDto> GetOptionsAsync(CancellationToken cancellationToken) =>
        _service.GetOptionsAsync(cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Organization.List)]
    [HttpGet("departments")]
    public Task<List<OaDepartmentOptionDto>> GetDepartmentsAsync(CancellationToken cancellationToken) => _service.GetDepartmentsAsync(cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Organization.List)]
    [HttpGet("departments/{departmentId:guid}")]
    public Task<OaDepartmentDetailsDto> GetDepartmentAsync(Guid departmentId, CancellationToken cancellationToken) => _service.GetDepartmentAsync(departmentId, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Organization.List)]
    [HttpGet("departments/{departmentId:guid}/members")]
    public Task<PagedResultDto<OaDepartmentMemberDto>> GetMembersAsync(Guid departmentId, [FromQuery] OaDepartmentMemberQuery input, CancellationToken cancellationToken) => _service.GetMembersAsync(departmentId, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Organization.List)]
    [HttpGet("users/search")]
    public Task<List<OaUserOptionDto>> SearchUsersAsync([FromQuery] string? keyword, CancellationToken cancellationToken) => _service.SearchUsersAsync(keyword, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Organization.Manage)]
    [HttpPost("departments")]
    public Task<Guid> CreateDepartmentAsync([FromBody] OaSaveDepartmentRequest input, CancellationToken cancellationToken) => _service.CreateDepartmentAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Organization.Manage)]
    [HttpPut("departments/{departmentId:guid}")]
    public Task UpdateDepartmentAsync(Guid departmentId, [FromBody] OaSaveDepartmentRequest input, CancellationToken cancellationToken) => _service.UpdateDepartmentAsync(departmentId, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Organization.Manage)]
    [HttpDelete("departments/{departmentId:guid}")]
    public Task DeleteDepartmentAsync(Guid departmentId, CancellationToken cancellationToken) => _service.DeleteDepartmentAsync(departmentId, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Organization.Manage)]
    [HttpPost("departments/{departmentId:guid}/members")]
    public Task AddMembersAsync(Guid departmentId, [FromBody] OaAddDepartmentMembersRequest input, CancellationToken cancellationToken) => _service.AddMembersAsync(departmentId, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Organization.Manage)]
    [HttpDelete("departments/{departmentId:guid}/members/{userId:guid}")]
    public Task RemoveMemberAsync(Guid departmentId, Guid userId, CancellationToken cancellationToken) => _service.RemoveMemberAsync(departmentId, userId, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Organization.Manage)]
    [HttpPut("departments/{departmentId:guid}/leader")]
    public Task SetLeaderAsync(Guid departmentId, [FromBody] OaSetDepartmentLeaderRequest input, CancellationToken cancellationToken) => _service.SetLeaderAsync(departmentId, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Organization.Manage)]
    [HttpPut("departments/{departmentId:guid}/members/{userId:guid}/manager")]
    public Task SetMemberManagerAsync(Guid departmentId, Guid userId, [FromBody] OaSetMemberManagerRequest input, CancellationToken cancellationToken) =>
        _service.SetMemberManagerAsync(departmentId, userId, input, cancellationToken);
}

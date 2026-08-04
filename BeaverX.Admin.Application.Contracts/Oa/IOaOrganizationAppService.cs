using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Oa;

public interface IOaOrganizationAppService
{
    Task<OaOrganizationOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default);
    Task<List<OaDepartmentOptionDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
    Task<OaDepartmentDetailsDto> GetDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<OaDepartmentMemberDto>> GetMembersAsync(Guid departmentId, OaDepartmentMemberQuery input, CancellationToken cancellationToken = default);
    Task<List<OaUserOptionDto>> SearchUsersAsync(string? keyword, CancellationToken cancellationToken = default);
    Task<Guid> CreateDepartmentAsync(OaSaveDepartmentRequest input, CancellationToken cancellationToken = default);
    Task UpdateDepartmentAsync(Guid departmentId, OaSaveDepartmentRequest input, CancellationToken cancellationToken = default);
    Task DeleteDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task AddMembersAsync(Guid departmentId, OaAddDepartmentMembersRequest input, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(Guid departmentId, Guid userId, CancellationToken cancellationToken = default);
    Task SetLeaderAsync(Guid departmentId, OaSetDepartmentLeaderRequest input, CancellationToken cancellationToken = default);
    Task SetMemberManagerAsync(Guid departmentId, Guid userId, OaSetMemberManagerRequest input, CancellationToken cancellationToken = default);
}

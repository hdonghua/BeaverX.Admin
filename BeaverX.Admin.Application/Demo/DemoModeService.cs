using BeaverX.Admin.Application.Contracts.Demo;
using BeaverX.Admin.Domain.Shared;
using BeaverX.Admin.Domain.Shared.Demo;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Core.Dependency;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Application.Demo;

public class DemoModeService : IDemoModeService, ISingletonDependency
{
    private readonly DemoModeOptions _options;

    public DemoModeService(IOptions<DemoModeOptions> options)
    {
        _options = options.Value;
    }

    public bool IsEnabled => _options.Enabled;

    public void EnsureMenuWritable()
    {
        if (IsEnabled)
        {
            throw new BusinessException("演示模式下不允许修改菜单");
        }
    }

    public void EnsureAdminUserOperable(string? userName)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(userName))
        {
            return;
        }

        if (string.Equals(
                userName.Trim(),
                _options.ProtectedAdminUserName,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("演示模式下不允许操作默认管理员账号");
        }
    }

    public void EnsureSuperAdminRoleOperable(string? roleCode)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(roleCode))
        {
            return;
        }

        if (string.Equals(
                roleCode.Trim(),
                RbacPermissionCodes.SuperAdmin,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("演示模式下不允许操作超级管理员角色");
        }
    }
}

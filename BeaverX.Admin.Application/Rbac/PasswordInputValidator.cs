using BeaverX.Admin.Domain.Shared;
using BeaverX.Admin.Domain.Shared.Rbac;

namespace BeaverX.Admin.Application.Rbac;

internal static class PasswordInputValidator
{
    public static void Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new BusinessException("密码不能为空");
        }

        if (!PasswordRules.IsValid(password))
        {
            throw new BusinessException(PasswordRules.ErrorMessage);
        }
    }
}

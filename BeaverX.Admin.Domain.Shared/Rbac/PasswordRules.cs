using System.Text.RegularExpressions;

namespace BeaverX.Admin.Domain.Shared.Rbac;

public static class PasswordRules
{
    public const int MinLength = 8;
    public const int MaxLength = 32;

    public const string ErrorMessage =
        "密码长度须为 8-32 位，且须包含大小写字母、数字和特殊字符";

    private static readonly Regex Pattern = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9\s]).{8,32}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsValid(string password) => Pattern.IsMatch(password);
}

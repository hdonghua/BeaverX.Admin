using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;

namespace BeaverX.Admin.Application.Rbac;

internal static class MenuInputValidator
{
    public static void Validate(
        MenuType menuType,
        string? path,
        string? component,
        bool isExternal)
    {
        if (isExternal)
        {
            if (menuType != MenuType.Menu)
            {
                throw new RbacException("外链仅支持菜单类型");
            }

            if (!IsExternalUrl(path))
            {
                throw new RbacException("外链地址必须是有效的 http/https URL");
            }

            return;
        }

        if (menuType == MenuType.Menu && string.IsNullOrWhiteSpace(path))
        {
            throw new RbacException("路由路径不能为空");
        }

        if (!string.IsNullOrWhiteSpace(path) && IsExternalUrl(path))
        {
            throw new RbacException("内部菜单不能使用外链地址，请开启外链开关");
        }

        if (menuType != MenuType.Menu && !string.IsNullOrWhiteSpace(component))
        {
            throw new RbacException("仅菜单类型可配置组件路径");
        }
    }

    public static bool IsExternalUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return Uri.TryCreate(path.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public static string? NormalizePath(string? path, bool isExternal) =>
        string.IsNullOrWhiteSpace(path) ? null : path.Trim();

    public static string? NormalizeComponent(string? component, bool isExternal) =>
        isExternal ? null : string.IsNullOrWhiteSpace(component) ? null : component.Trim();
}

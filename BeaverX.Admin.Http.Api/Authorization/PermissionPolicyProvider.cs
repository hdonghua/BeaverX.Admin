using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Http.Api.Authorization;

public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    public const string PolicyPrefix = "Permission:";
    public const string MultiPolicyPrefix = "PermissionSet:";
    private const char PermissionDelimiter = '\x1f';

    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public static string BuildPolicyName(
        IReadOnlyList<string> permissions,
        PermissionMatchMode match)
    {
        if (permissions.Count == 0)
        {
            throw new ArgumentException("At least one permission code is required.", nameof(permissions));
        }

        if (permissions.Count == 1)
        {
            return $"{PolicyPrefix}{permissions[0]}";
        }

        var modeToken = match == PermissionMatchMode.All ? "all" : "any";
        return $"{MultiPolicyPrefix}{modeToken}:{string.Join(PermissionDelimiter, permissions)}";
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallbackPolicyProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal))
        {
            var permission = policyName[PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        if (policyName.StartsWith(MultiPolicyPrefix, StringComparison.Ordinal))
        {
            var payload = policyName[MultiPolicyPrefix.Length..];
            var separatorIndex = payload.IndexOf(':');
            if (separatorIndex <= 0)
            {
                return _fallbackPolicyProvider.GetPolicyAsync(policyName);
            }

            var modeToken = payload[..separatorIndex];
            var permissions = payload[(separatorIndex + 1)..]
                .Split(PermissionDelimiter, StringSplitOptions.RemoveEmptyEntries);

            if (permissions.Length == 0)
            {
                return _fallbackPolicyProvider.GetPolicyAsync(policyName);
            }

            var match = string.Equals(modeToken, "all", StringComparison.OrdinalIgnoreCase)
                ? PermissionMatchMode.All
                : PermissionMatchMode.Any;

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permissions, match))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}

using System.Text.Json;
using BeaverX.Admin.Application.Contracts.Exports;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Exports;
using BeaverX.Core.Dependency;
using BeaverX.Data.SqlSugar.Repositories;
using MiniExcelLibs;

namespace BeaverX.Admin.Application.Exports.Handlers;

public class UserExportHandler : IExportHandler, IScopedDependency
{
    private const int MaxRows = 100_000;

    private readonly ISugarRepository<User> _userRepository;

    public UserExportHandler(ISugarRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    public string ExportType => ExportTypes.SystemUser;

    public string DisplayName => "用户列表";

    public async Task<ExportHandlerResult> ExportAsync(
        string? parametersJson,
        CancellationToken cancellationToken = default)
    {
        var query = _userRepository.GetSugarQueryable();

        if (!string.IsNullOrWhiteSpace(parametersJson))
        {
            var parameters = JsonSerializer.Deserialize<UserExportParameters>(
                parametersJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!string.IsNullOrWhiteSpace(parameters?.Keyword))
            {
                var keyword = parameters.Keyword.Trim();
                query = query.Where(x =>
                    x.UserName.Contains(keyword) ||
                    (x.NickName != null && x.NickName.Contains(keyword)) ||
                    (x.Email != null && x.Email.Contains(keyword)));
            }

            if (parameters?.IsEnabled is bool isEnabled)
            {
                query = query.Where(x => x.IsEnabled == isEnabled);
            }
        }

        var users = await query
            .OrderByDescending(x => x.CreationTime)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(x => x.Id).ToList();
        var roleLinks = userIds.Count == 0
            ? []
            : await _userRepository.Client.Queryable<UserRole>()
                .Where(x => userIds.Contains(x.UserId))
                .ToListAsync(cancellationToken);
        var roleIds = roleLinks.Select(x => x.RoleId).Distinct().ToList();
        var roles = roleIds.Count == 0
            ? []
            : await _userRepository.Client.Queryable<Role>()
                .Where(x => roleIds.Contains(x.Id))
                .ToListAsync(cancellationToken);
        var roleMap = roles.ToDictionary(x => x.Id, x => x.Name);
        var userRoleNames = roleLinks
            .Where(x => roleMap.ContainsKey(x.RoleId))
            .GroupBy(x => x.UserId)
            .ToDictionary(
                x => x.Key,
                x => string.Join("、", x.Select(link => roleMap[link.RoleId])));

        var rows = users
            .Select(x => new
            {
                账号 = x.UserName,
                昵称 = x.NickName ?? string.Empty,
                邮箱 = x.Email ?? string.Empty,
                手机 = x.Phone ?? string.Empty,
                角色 = userRoleNames.GetValueOrDefault(x.Id, string.Empty),
                状态 = x.IsEnabled ? "启用" : "禁用",
                创建时间 = x.CreationTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
            })
            .ToList();

        var stream = new MemoryStream();
        await stream.SaveAsAsync(rows, cancellationToken: cancellationToken);
        stream.Position = 0;

        return new ExportHandlerResult
        {
            Content = stream,
            FileName = $"{DisplayName}_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
        };
    }

    private sealed class UserExportParameters
    {
        public string? Keyword { get; set; }
        public bool? IsEnabled { get; set; }
    }
}

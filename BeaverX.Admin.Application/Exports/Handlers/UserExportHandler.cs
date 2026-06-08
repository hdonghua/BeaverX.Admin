using System.Text.Json;
using BeaverX.Admin.Application.Contracts.Exports;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Exports;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;

namespace BeaverX.Admin.Application.Exports.Handlers;

public class UserExportHandler : IExportHandler, IScopedDependency
{
    private const int MaxRows = 100_000;

    private readonly IRepository<User> _userRepository;

    public UserExportHandler(IRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    public string ExportType => ExportTypes.SystemUser;

    public string DisplayName => "用户列表";

    public async Task<ExportHandlerResult> ExportAsync(
        string? parametersJson,
        CancellationToken cancellationToken = default)
    {
        var query = _userRepository.GetQueryable()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .AsQueryable();

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

        var rows = await query
            .OrderByDescending(x => x.CreationTime)
            .Take(MaxRows)
            .Select(x => new
            {
                账号 = x.UserName,
                昵称 = x.NickName ?? string.Empty,
                邮箱 = x.Email ?? string.Empty,
                手机 = x.Phone ?? string.Empty,
                角色 = string.Join("、", x.UserRoles.Select(r => r.Role.Name)),
                状态 = x.IsEnabled ? "启用" : "禁用",
                创建时间 = x.CreationTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
            })
            .ToListAsync(cancellationToken);

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

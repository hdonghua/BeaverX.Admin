using System.Text.Json;
using BeaverX.Admin.Application.Contracts.Exports;
using BeaverX.Admin.Domain.Config;
using BeaverX.Admin.Domain.Shared.Exports;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;

namespace BeaverX.Admin.Application.Exports.Handlers;

public class ConfigExportHandler : IExportHandler, IScopedDependency
{
    private const int MaxRows = 100_000;

    private readonly IRepository<SysConfig> _configRepository;

    public ConfigExportHandler(IRepository<SysConfig> configRepository)
    {
        _configRepository = configRepository;
    }

    public string ExportType => ExportTypes.SystemConfig;

    public string DisplayName => "系统配置";

    public async Task<ExportHandlerResult> ExportAsync(
        string? parametersJson,
        CancellationToken cancellationToken = default)
    {
        var query = _configRepository.GetQueryable().AsQueryable();

        if (!string.IsNullOrWhiteSpace(parametersJson))
        {
            var parameters = JsonSerializer.Deserialize<ConfigExportParameters>(
                parametersJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!string.IsNullOrWhiteSpace(parameters?.Keyword))
            {
                var keyword = parameters.Keyword.Trim();
                query = query.Where(x =>
                    x.Key.Contains(keyword) ||
                    x.Label.Contains(keyword) ||
                    x.Value.Contains(keyword));
            }

            if (parameters?.Group != null)
            {
                if (string.IsNullOrWhiteSpace(parameters.Group))
                {
                    query = query.Where(x => x.Group == null || x.Group == string.Empty);
                }
                else
                {
                    query = query.Where(x => x.Group == parameters.Group.Trim());
                }
            }
        }

        var rows = await query
            .OrderBy(x => x.Group)
            .ThenBy(x => x.Sort)
            .Take(MaxRows)
            .Select(x => new
            {
                配置键 = x.Key,
                标签 = x.Label,
                配置值 = x.Value,
                分组 = x.Group ?? string.Empty,
                排序 = x.Sort,
                状态 = x.IsEnabled ? "启用" : "禁用",
                备注 = x.Remark ?? string.Empty
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

    private sealed class ConfigExportParameters
    {
        public string? Keyword { get; set; }
        public string? Group { get; set; }
    }
}

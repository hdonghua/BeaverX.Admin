using System.Text.Json;
using BeaverX.Admin.Application.Contracts.Exports;
using BeaverX.Admin.Domain.Dict;
using BeaverX.Admin.Domain.Shared.Exports;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;

namespace BeaverX.Admin.Application.Exports.Handlers;

public class DictDataExportHandler : IExportHandler, IScopedDependency
{
    private const int MaxRows = 100_000;

    private readonly IRepository<DictData, Guid> _dictDataRepository;

    public DictDataExportHandler(IRepository<DictData, Guid> dictDataRepository)
    {
        _dictDataRepository = dictDataRepository;
    }

    public string ExportType => ExportTypes.SystemDictData;

    public string DisplayName => "字典数据";

    public async Task<ExportHandlerResult> ExportAsync(
        string? parametersJson,
        CancellationToken cancellationToken = default)
    {
        var query = (await _dictDataRepository.GetQueryableAsync())
            .Include(x => x.DictType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(parametersJson))
        {
            var parameters = JsonSerializer.Deserialize<DictDataExportParameters>(
                parametersJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parameters?.DictTypeId is Guid dictTypeId && dictTypeId != Guid.Empty)
            {
                query = query.Where(x => x.DictTypeId == dictTypeId);
            }

            if (!string.IsNullOrWhiteSpace(parameters?.TypeCode))
            {
                var typeCode = parameters.TypeCode.Trim();
                query = query.Where(x => x.DictType.Code == typeCode);
            }

            if (!string.IsNullOrWhiteSpace(parameters?.Keyword))
            {
                var keyword = parameters.Keyword.Trim();
                query = query.Where(x => x.Label.Contains(keyword) || x.Value.Contains(keyword));
            }
        }

        var rows = await query
            .OrderBy(x => x.DictType.Code)
            .ThenBy(x => x.Sort)
            .Take(MaxRows)
            .Select(x => new
            {
                字典类型 = x.DictType.Name,
                类型编码 = x.DictType.Code,
                标签 = x.Label,
                值 = x.Value,
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

    private sealed class DictDataExportParameters
    {
        public Guid? DictTypeId { get; set; }
        public string? TypeCode { get; set; }
        public string? Keyword { get; set; }
    }
}

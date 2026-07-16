using System.Text.Json;
using BeaverX.Admin.Application.Contracts.Exports;
using BeaverX.Admin.Domain.Dict;
using BeaverX.Admin.Domain.Shared.Exports;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using MiniExcelLibs;

namespace BeaverX.Admin.Application.Exports.Handlers;

public class DictDataExportHandler : IExportHandler, IScopedDependency
{
    private const int MaxRows = 100_000;

    private readonly ISugarRepository<DictData> _dictDataRepository;

    public DictDataExportHandler(ISugarRepository<DictData> dictDataRepository)
    {
        _dictDataRepository = dictDataRepository;
    }

    public string ExportType => ExportTypes.SystemDictData;

    public string DisplayName => "字典数据";

    public async Task<ExportHandlerResult> ExportAsync(
        string? parametersJson,
        CancellationToken cancellationToken = default)
    {
        var query = _dictDataRepository.GetSugarQueryable();

        if (!string.IsNullOrWhiteSpace(parametersJson))
        {
            var parameters = JsonSerializer.Deserialize<DictDataExportParameters>(
                parametersJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parameters?.DictTypeId is > 0)
            {
                var dictTypeId = parameters.DictTypeId.Value;
                query = query.Where(x => x.DictTypeId == dictTypeId);
            }

            if (!string.IsNullOrWhiteSpace(parameters?.TypeCode))
            {
                var typeCode = parameters.TypeCode.Trim();
                var dictTypeId = await _dictDataRepository.Client.Queryable<DictType>()
                    .Where(x => x.Code == typeCode)
                    .Select(x => x.Id)
                    .FirstAsync(cancellationToken);
                query = query.Where(x => x.DictTypeId == dictTypeId);
            }

            if (!string.IsNullOrWhiteSpace(parameters?.Keyword))
            {
                var keyword = parameters.Keyword.Trim();
                query = query.Where(x => x.Label.Contains(keyword) || x.Value.Contains(keyword));
            }
        }

        var items = await query
            .OrderBy(x => x.DictTypeId)
            .OrderBy(x => x.Sort)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var dictTypeIds = items.Select(x => x.DictTypeId).Distinct().ToList();
        var dictTypes = dictTypeIds.Count == 0
            ? []
            : await _dictDataRepository.Client.Queryable<DictType>()
                .Where(x => dictTypeIds.Contains(x.Id))
                .ToListAsync(cancellationToken);
        var dictTypeMap = dictTypes.ToDictionary(x => x.Id);

        var rows = items.Select(x =>
        {
            dictTypeMap.TryGetValue(x.DictTypeId, out var dictType);
            return new
            {
                字典类型 = dictType?.Name ?? string.Empty,
                类型编码 = dictType?.Code ?? string.Empty,
                标签 = x.Label,
                值 = x.Value,
                排序 = x.Sort,
                状态 = x.IsEnabled ? "启用" : "禁用",
                备注 = x.Remark ?? string.Empty
            };
        }).ToList();

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
        public long? DictTypeId { get; set; }
        public string? TypeCode { get; set; }
        public string? Keyword { get; set; }
    }
}

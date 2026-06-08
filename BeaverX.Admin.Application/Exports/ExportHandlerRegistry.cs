using BeaverX.Admin.Application.Contracts.Exports;
using BeaverX.Core.Dependency;

namespace BeaverX.Admin.Application.Exports;

public class ExportHandlerRegistry : IScopedDependency
{
    private readonly IReadOnlyDictionary<string, IExportHandler> _handlers;

    public ExportHandlerRegistry(IEnumerable<IExportHandler> handlers)
    {
        _handlers = handlers.ToDictionary(x => x.ExportType, StringComparer.OrdinalIgnoreCase);
    }

    public IExportHandler GetRequired(string exportType)
    {
        if (!_handlers.TryGetValue(exportType, out var handler))
        {
            throw new InvalidOperationException($"未注册的导出类型: {exportType}");
        }

        return handler;
    }

    public bool TryGet(string exportType, out IExportHandler? handler) =>
        _handlers.TryGetValue(exportType, out handler);
}

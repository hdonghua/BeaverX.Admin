using System.Text.Json;
using BeaverX.Admin.Application.Contracts.Oa;
using BeaverX.Admin.Domain.Oa;
using BeaverX.Admin.Domain.Shared.Oa;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace BeaverX.Admin.Application.Oa;

[ExposeServices(typeof(IOaWorkflowLaunchService))]
public sealed class OaWorkflowLaunchService : IOaWorkflowLaunchService, IScopedDependency
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IRepository<OaProcessDefinition, Guid> _definitions;
    private readonly IOaWorkflowInstanceAppService _workflowInstances;

    public OaWorkflowLaunchService(
        IRepository<OaProcessDefinition, Guid> definitions,
        IOaWorkflowInstanceAppService workflowInstances)
    {
        _definitions = definitions;
        _workflowInstances = workflowInstances;
    }

    public async Task<OaWorkflowLaunchResult> LaunchAsync(
        OaWorkflowLaunchRequest input,
        CancellationToken cancellationToken = default)
    {
        var processKey = input.ProcessKey?.Trim();
        if (string.IsNullOrWhiteSpace(processKey)) throw new BusinessException("流程 Key 不能为空");

        var normalizedKey = processKey.ToLower();
        var definition = await (await _definitions.GetQueryableAsync())
            .AsNoTracking()
            .Where(x => x.BelongKey.ToLower() == normalizedKey && x.Status == OaDefinitionStatus.Published)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessException($"未找到 Key 为“{processKey}”的已发布流程");

        var instanceId = await _workflowInstances.LaunchAsync(new OaLaunchRequest
        {
            FlowDefId = definition.Id,
            FlowValue = JsonSerializer.Serialize(input.FormData ?? [], JsonOptions)
        }, cancellationToken);

        return new OaWorkflowLaunchResult
        {
            InstanceId = instanceId,
            DefinitionId = definition.Id,
            ProcessKey = definition.BelongKey
        };
    }
}

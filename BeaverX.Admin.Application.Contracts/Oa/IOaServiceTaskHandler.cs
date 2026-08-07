using Volo.Abp.DependencyInjection;

namespace BeaverX.Admin.Application.Contracts.Oa;

/// <summary>
/// A code handler that can be selected by a workflow service-task node.
/// </summary>
public interface IOaServiceTaskHandler : ITransientDependency
{
    string Key { get; }
    string DisplayName { get; }
    Task HandleAsync(OaServiceTaskContext context, CancellationToken cancellationToken = default);
}

public sealed class OaServiceTaskContext
{
    public Guid InstanceId { get; init; }
    public Guid DefinitionId { get; init; }
    public Guid NodeId { get; init; }
    public Guid InitiatorId { get; init; }
    public Dictionary<string, object?> FormData { get; init; } = [];
}

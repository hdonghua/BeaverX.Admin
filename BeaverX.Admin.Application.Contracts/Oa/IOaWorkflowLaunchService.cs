namespace BeaverX.Admin.Application.Contracts.Oa;

/// <summary>
/// Starts workflow instances from business application code.
/// </summary>
public interface IOaWorkflowLaunchService
{
    Task<OaWorkflowLaunchResult> LaunchAsync(
        OaWorkflowLaunchRequest input,
        CancellationToken cancellationToken = default);
}

public sealed class OaWorkflowLaunchRequest
{
    /// <summary>
    /// Stable process key exposed as LinkId. The key remains unchanged across process versions.
    /// </summary>
    public string ProcessKey { get; set; } = null!;

    /// <summary>
    /// Form values keyed by the workflow form FieldKey.
    /// </summary>
    public Dictionary<string, object?> FormData { get; set; } = [];
}

public sealed class OaWorkflowLaunchResult
{
    public Guid InstanceId { get; set; }
    public Guid DefinitionId { get; set; }
    public string ProcessKey { get; set; } = null!;
}


namespace BeaverX.Admin.Domain.Shared.Oa;

/// <summary>
/// Stable workflow keys referenced by business application code.
/// </summary>
public static class OaWorkflowKeys
{
    public const string PurchaseRequest = "purchase_request";

    public static IReadOnlyDictionary<string, string> Options { get; } =
        new Dictionary<string, string>
        {
            [PurchaseRequest] = "采购申请"
        };
}

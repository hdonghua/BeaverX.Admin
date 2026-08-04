namespace BeaverX.Admin.Domain.Shared.Oa;

public enum OaInstanceStatus
{
    /// <summary>
    /// 审批中
    /// </summary>
    Underway = 0,

    /// <summary>
    /// 已通过
    /// </summary>
    Approved = 1,

    /// <summary>
    /// 不通过
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// 已撤销
    /// </summary>
    Cancelled = 3
}

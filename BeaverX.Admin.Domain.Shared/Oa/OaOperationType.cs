namespace BeaverX.Admin.Domain.Shared.Oa;

public enum OaOperationType
{
    /// <summary>
    /// 发起
    /// </summary>
    Start = 0,

    /// <summary>
    /// 自动拒绝
    /// </summary>
    AutoRejected = 1,

    /// <summary>
    /// 自动通过
    /// </summary>
    AutoApproved = 2,

    /// <summary>
    /// 拒绝
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// 通过
    /// </summary>
    Approved = 4,

    /// <summary>
    /// 撤销
    /// </summary>
    Canceled = 5,

    /// <summary>
    /// 转交
    /// </summary>
    Assign = 6,

    /// <summary>
    /// 回退
    /// </summary>
    Back = 7,

    /// <summary>
    /// 加签
    /// </summary>
    AddSign = 8,

    /// <summary>
    /// 减签  
    /// </summary>
    DelSign = 9,

    /// <summary>
    /// 前加签
    /// </summary>
    AddBeforeSign = 10,

    /// <summary>
    /// 后加签
    /// </summary>
    AddAfterSign = 11,

    /// <summary>
    /// 抄送
    /// </summary>
    Copy = 12,

    /// <summary>
    /// 转发
    /// </summary>
    Forward = 13,

    /// <summary>
    /// 评论
    /// </summary>
    Comment = 14,

    /// <summary>
    /// 办理
    /// </summary>
    Transact = 15,

    /// <summary>
    /// 转办
    /// </summary>
    Transfer = 16, 

    FormModified = 17,

    Urge = 18,

    /// <summary>
    /// 服务任务自动执行
    /// </summary>
    ServiceTask = 19
}

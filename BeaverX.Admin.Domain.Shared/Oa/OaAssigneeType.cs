namespace BeaverX.Admin.Domain.Shared.Oa;

public enum OaAssigneeType
{
    /// <summary>
    /// 发起人本人
    /// </summary>
    Self = 0,

    /// <summary>
    /// 上级
    /// </summary>
    Superior = 1,

    /// <summary>
    /// 部门负责人
    /// </summary>
    DepartmentLeader = 2,

    /// <summary>
    /// 角色
    /// </summary>
    Role = 3,

    /// <summary>
    /// 指定成员
    /// </summary>
    Assignee = 4,

    /// <summary>
    /// 连续多级上级
    /// </summary>
    MultistepLeader = 5,

    /// <summary>
    /// 连续多级部门负责人
    /// </summary>
    MultistepDepartmentLeader = 6,

    /// <summary>
    /// 发起人自选
    /// </summary>
    InitiatorChoice = 7
}
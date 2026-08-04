namespace BeaverX.Admin.Domain.Shared.Oa;

public enum OaDefinitionStatus { Draft = 0, Published = 1, Disabled = 2 }
public enum OaInstanceStatus { Underway = 0, Approved = 1, Rejected = 2, Cancelled = 3 }
public enum OaTaskStatus { Pending = 0, Approved = 1, Rejected = 2, Transferred = 3, Recalled = 4 }
public enum OaNodeType { Start = 0, Approve = 1, Copy = 2, Condition = 3, ExclusiveGateway = 4, Transact = 5, Trigger = 6, End = 9 }
public enum OaOperationType { Start = 0, AutoRejected = 1, AutoApproved = 2, Rejected = 3, Approved = 4, Canceled = 5, Assign = 6, Back = 7, AddSign = 8, DelSign = 9, AddBeforeSign = 10, AddAfterSign = 11, Copy = 12, Forward = 13, Comment = 14, Transact = 15, Transfer = 16, FormModified = 17, Urge = 18 }
public enum OaPermissionType { All = 0, Specified = 1, None = 2 }
public enum OaAssigneeType { Self = 0, Superior = 1, DepartmentLeader = 2, Role = 3, Assignee = 4, MultistepLeader = 5, MultistepDepartmentLeader = 6, InitiatorChoice = 7 }

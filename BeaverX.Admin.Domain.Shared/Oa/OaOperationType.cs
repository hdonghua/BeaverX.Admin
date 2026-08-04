namespace BeaverX.Admin.Domain.Shared.Oa;

public enum OaOperationType
{
    Start = 0,
    AutoRejected = 1,
    AutoApproved = 2,
    Rejected = 3, 
    Approved = 4, 
    Canceled = 5,
    Assign = 6, 
    Back = 7, 
    AddSign = 8,
    DelSign = 9,
    AddBeforeSign = 10,
    AddAfterSign = 11,
    Copy = 12, 
    Forward = 13,
    Comment = 14, 
    Transact = 15,
    Transfer = 16, 
    FormModified = 17,
    Urge = 18
}

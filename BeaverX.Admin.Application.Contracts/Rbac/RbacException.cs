namespace BeaverX.Admin.Application.Contracts.Rbac;

public class RbacException : Exception
{
    public RbacException(string message) : base(message)
    {
    }
}

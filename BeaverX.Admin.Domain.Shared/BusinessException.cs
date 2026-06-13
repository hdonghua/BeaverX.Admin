namespace BeaverX.Admin.Domain.Shared;

/// <summary>
/// 业务规则校验失败时抛出，由 API 层统一转换为 400 响应。
/// </summary>
public class BusinessException : Exception
{
    public BusinessException(string message)
        : base(message)
    {
    }
}

namespace BeaverX.Admin.Application.Contracts.Payment;

/// <summary>按渠道编码解析 <see cref="IPaymentProvider"/> 实现</summary>
public interface IPaymentProviderResolver
{
    IPaymentProvider Resolve(string channelCode);
}

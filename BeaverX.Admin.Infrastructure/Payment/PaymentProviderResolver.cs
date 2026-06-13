using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Domain.Shared;
using BeaverX.Core.Dependency;

namespace BeaverX.Admin.Infrastructure.Payment;

/// <summary>按渠道编码解析已注册的 <see cref="IPaymentProvider"/> 实现</summary>
public class PaymentProviderResolver : IPaymentProviderResolver, IScopedDependency
{
  private readonly IEnumerable<IPaymentProvider> _providers;

  public PaymentProviderResolver(IEnumerable<IPaymentProvider> providers)
  {
    _providers = providers;
  }

  public IPaymentProvider Resolve(string channelCode)
  {
    var provider = _providers.FirstOrDefault(x =>
      string.Equals(x.ChannelCode, channelCode, StringComparison.OrdinalIgnoreCase));

    if (provider == null)
    {
      throw new BusinessException($"未注册的支付渠道: {channelCode}");
    }

    return provider;
  }
}

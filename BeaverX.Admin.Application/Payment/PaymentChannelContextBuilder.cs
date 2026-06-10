using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Core.Dependency;

namespace BeaverX.Admin.Application.Payment;

public class PaymentChannelContextBuilder : IPaymentChannelContextBuilder, IScopedDependency
{
  private readonly IPaymentChannelCertMaterializer _certMaterializer;

  public PaymentChannelContextBuilder(IPaymentChannelCertMaterializer certMaterializer)
  {
    _certMaterializer = certMaterializer;
  }

  public async Task<PaymentProviderChannelContext> BuildAsync(
    long channelId,
    string channelCode,
    PaymentProviderType providerType,
    string configJson,
    CancellationToken cancellationToken = default)
  {
    var resolvedConfigJson = configJson;
    if (providerType is PaymentProviderType.Alipay or PaymentProviderType.AlipayApp)
    {
      resolvedConfigJson = await _certMaterializer.ResolveAlipayConfigJsonAsync(
        channelId,
        configJson,
        cancellationToken);
    }

    return new PaymentProviderChannelContext
    {
      ChannelId = channelId,
      ChannelCode = channelCode,
      ProviderType = providerType,
      ConfigJson = resolvedConfigJson,
    };
  }
}

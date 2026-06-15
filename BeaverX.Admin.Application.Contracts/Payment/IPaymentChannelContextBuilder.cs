using BeaverX.Admin.Domain.Shared.Payment;

namespace BeaverX.Admin.Application.Contracts.Payment;

public interface IPaymentChannelContextBuilder
{
    Task<PaymentProviderChannelContext> BuildAsync(
      long channelId,
      string channelCode,
      PaymentProviderType providerType,
      string configJson,
      CancellationToken cancellationToken = default);
}

namespace BeaverX.Admin.Application.Contracts.Payment;

public interface IPaymentProvider
{
  string ChannelCode { get; }

  Task<NativePayResult> CreateNativePayAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    string notifyUrl,
    CancellationToken cancellationToken = default);

  Task<QueryPayResult> QueryPayAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    CancellationToken cancellationToken = default);

  Task<RefundProviderResult> RefundAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    PaymentProviderRefundContext refund,
    string notifyUrl,
    CancellationToken cancellationToken = default);

  Task<NotifyHandleResult> HandlePayNotifyAsync(
    PaymentProviderChannelContext channel,
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default);

  Task<NotifyHandleResult> HandleRefundNotifyAsync(
    PaymentProviderChannelContext channel,
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default);
}

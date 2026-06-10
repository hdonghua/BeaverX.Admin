using BeaverX.Admin.Application.Contracts.Payment;

namespace BeaverX.Admin.Infrastructure.Payment;

/// <summary>支付 Provider 基类，为未实现的支付方式提供默认「不支持」响应</summary>
public abstract class PaymentProviderBase : IPaymentProvider
{
  public abstract string ChannelCode { get; }

  public virtual Task<QrcodePayResult> CreateQrcodePayAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    string notifyUrl,
    CancellationToken cancellationToken = default)
    => Task.FromResult(QrcodePayResult.Fail("NOT_SUPPORTED", $"渠道 {ChannelCode} 不支持二维码支付"));

  public virtual Task<AppPayResult> CreateAppPayAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    string notifyUrl,
    CancellationToken cancellationToken = default)
    => Task.FromResult(AppPayResult.Fail("NOT_SUPPORTED", $"渠道 {ChannelCode} 不支持 App 支付"));

  public abstract Task<QueryPayResult> QueryPayAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    CancellationToken cancellationToken = default);

  public abstract Task<RefundProviderResult> RefundAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    PaymentProviderRefundContext refund,
    string notifyUrl,
    CancellationToken cancellationToken = default);

  public abstract Task<NotifyHandleResult> HandlePayNotifyAsync(
    PaymentProviderChannelContext channel,
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default);

  public abstract Task<NotifyHandleResult> HandleRefundNotifyAsync(
    PaymentProviderChannelContext channel,
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default);
}

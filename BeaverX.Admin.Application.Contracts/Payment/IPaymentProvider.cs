namespace BeaverX.Admin.Application.Contracts.Payment;

/// <summary>
/// 支付渠道 Provider 契约。
/// 二维码渠道实现 <see cref="CreateQrcodePayAsync"/>，App 渠道实现 <see cref="CreateAppPayAsync"/>。
/// </summary>
public interface IPaymentProvider
{
  /// <summary>渠道编码，与 <see cref="Domain.Shared.Payment.PaymentChannelCodes"/> 一致</summary>
  string ChannelCode { get; }

  /// <summary>创建二维码支付（微信扫码 / 支付宝当面付）</summary>
  Task<QrcodePayResult> CreateQrcodePayAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    string notifyUrl,
    CancellationToken cancellationToken = default);

  /// <summary>创建 App 调起支付（支付宝 App）</summary>
  Task<AppPayResult> CreateAppPayAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    string notifyUrl,
    CancellationToken cancellationToken = default);

  /// <summary>向渠道查询订单支付状态</summary>
  Task<QueryPayResult> QueryPayAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    CancellationToken cancellationToken = default);

  /// <summary>向渠道发起退款</summary>
  Task<RefundProviderResult> RefundAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    PaymentProviderRefundContext refund,
    string notifyUrl,
    CancellationToken cancellationToken = default);

  /// <summary>验签并解析支付成功回调</summary>
  Task<NotifyHandleResult> HandlePayNotifyAsync(
    PaymentProviderChannelContext channel,
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default);

  /// <summary>验签并解析退款回调</summary>
  Task<NotifyHandleResult> HandleRefundNotifyAsync(
    PaymentProviderChannelContext channel,
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default);
}

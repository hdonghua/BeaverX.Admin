namespace BeaverX.Admin.Domain.Shared.Payment;

/// <summary>支付回调日志类型</summary>
public static class PaymentNotifyType
{
  /// <summary>支付成功回调</summary>
  public const string Payment = "payment";

  /// <summary>退款结果回调</summary>
  public const string Refund = "refund";
}

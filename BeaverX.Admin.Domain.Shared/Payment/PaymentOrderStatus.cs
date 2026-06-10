namespace BeaverX.Admin.Domain.Shared.Payment;

/// <summary>支付订单状态</summary>
public enum PaymentOrderStatus
{
  /// <summary>待创建/待调起</summary>
  Pending = 0,

  /// <summary>待支付（已生成二维码或 App 参数）</summary>
  Paying = 1,

  /// <summary>支付成功</summary>
  Success = 2,

  /// <summary>创建支付失败</summary>
  Failed = 3,

  /// <summary>已关闭</summary>
  Closed = 4,

  /// <summary>退款中</summary>
  Refunding = 5,

  /// <summary>已全额退款</summary>
  Refunded = 6,

  /// <summary>部分退款</summary>
  PartialRefunded = 7,
}

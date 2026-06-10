namespace BeaverX.Admin.Domain.Shared.Payment;

/// <summary>退款单状态</summary>
public enum PaymentRefundStatus
{
  /// <summary>待提交渠道</summary>
  Pending = 0,

  /// <summary>渠道处理中</summary>
  Processing = 1,

  /// <summary>退款成功</summary>
  Success = 2,

  /// <summary>退款失败</summary>
  Failed = 3,
}

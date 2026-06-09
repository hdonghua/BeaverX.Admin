using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Payment;

/// <summary>
/// 退款单
/// </summary>
public class PaymentRefund : FullAuditedEntity
{
  public string RefundNo { get; set; } = null!;
  public long PaymentOrderId { get; set; }
  public string OrderNo { get; set; } = null!;
  public string ChannelCode { get; set; } = null!;
  /// <summary>退款金额（分）</summary>
  public long Amount { get; set; }
  /// <summary>原订单金额（分）</summary>
  public long TotalAmount { get; set; }
  public PaymentRefundStatus Status { get; set; } = PaymentRefundStatus.Pending;
  public string? ChannelRefundNo { get; set; }
  public string? ChannelOrderNo { get; set; }
  public string? Reason { get; set; }
  public DateTime? RefundTime { get; set; }
  public string? ErrorCode { get; set; }
  public string? ErrorMessage { get; set; }

  public PaymentOrder? PaymentOrder { get; set; }
}

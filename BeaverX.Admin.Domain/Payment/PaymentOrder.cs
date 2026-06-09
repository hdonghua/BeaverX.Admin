using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Payment;

/// <summary>
/// 支付订单（付款单）
/// </summary>
public class PaymentOrder : FullAuditedEntity
{
  public string OrderNo { get; set; } = null!;
  public string ChannelCode { get; set; } = null!;
  public string Subject { get; set; } = null!;
  public string? Description { get; set; }
  /// <summary>金额（分）</summary>
  public long Amount { get; set; }
  public string Currency { get; set; } = "CNY";
  public PaymentOrderStatus Status { get; set; } = PaymentOrderStatus.Pending;
  public string? ClientIp { get; set; }
  public string? Attach { get; set; }
  public string? BusinessType { get; set; }
  public string? BusinessId { get; set; }
  public long? UserId { get; set; }
  public DateTime? ExpireTime { get; set; }
  public DateTime? PaidTime { get; set; }
  public string? ChannelOrderNo { get; set; }
  public string? ChannelUserId { get; set; }
  public string? QrCodeUrl { get; set; }
  public long RefundedAmount { get; set; }
  public string? ErrorCode { get; set; }
  public string? ErrorMessage { get; set; }
}

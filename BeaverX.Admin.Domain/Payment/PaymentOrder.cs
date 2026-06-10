using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Payment;

/// <summary>支付订单（付款单）</summary>
public class PaymentOrder : FullAuditedEntity
{
  /// <summary>系统订单号（商户侧 out_trade_no）</summary>
  public string OrderNo { get; set; } = null!;

  /// <summary>支付渠道编码</summary>
  public string ChannelCode { get; set; } = null!;

  public string Subject { get; set; } = null!;
  public string? Description { get; set; }

  /// <summary>金额（分）</summary>
  public long Amount { get; set; }
  public string Currency { get; set; } = "CNY";
  public PaymentOrderStatus Status { get; set; } = PaymentOrderStatus.Pending;
  public string? ClientIp { get; set; }

  /// <summary>附加数据，回调时原样返回</summary>
  public string? Attach { get; set; }
  public string? BusinessType { get; set; }
  public string? BusinessId { get; set; }
  public long? UserId { get; set; }
  public DateTime? ExpireTime { get; set; }
  public DateTime? PaidTime { get; set; }

  /// <summary>渠道侧交易号（如微信 transaction_id）</summary>
  public string? ChannelOrderNo { get; set; }
  public string? ChannelUserId { get; set; }

  /// <summary>二维码支付链接（微信 code_url / 支付宝 qr_code）</summary>
  public string? QrCodeUrl { get; set; }

  /// <summary>支付宝 App 支付 orderString</summary>
  public string? AppPayOrderString { get; set; }

  /// <summary>累计已退款金额（分）</summary>
  public long RefundedAmount { get; set; }
  public string? ErrorCode { get; set; }
  public string? ErrorMessage { get; set; }
}

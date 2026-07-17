using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Payment;

/// <summary>
/// 支付/退款回调日志
/// </summary>
[SugarTable("pay_notify_logs")]
public class PaymentNotifyLog : Entity
{
  public string NotifyType { get; set; } = null!;
  public string ChannelCode { get; set; } = null!;
  public string? OrderNo { get; set; }
  public string? RefundNo { get; set; }
  public string RawBody { get; set; } = null!;
  public bool ProcessSuccess { get; set; }
  public string? ProcessMessage { get; set; }
  public DateTime CreatedTime { get; set; }
}

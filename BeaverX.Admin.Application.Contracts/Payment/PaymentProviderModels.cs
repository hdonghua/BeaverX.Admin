using BeaverX.Admin.Domain.Shared.Payment;

namespace BeaverX.Admin.Application.Contracts.Payment;

public class PaymentProviderChannelContext
{
  public string ChannelCode { get; set; } = null!;
  public PaymentProviderType ProviderType { get; set; }
  public string ConfigJson { get; set; } = "{}";
}

public class PaymentProviderOrderContext
{
  public string OrderNo { get; set; } = null!;
  public string Subject { get; set; } = null!;
  public string? Description { get; set; }
  public long Amount { get; set; }
  public string Currency { get; set; } = "CNY";
  public string? Attach { get; set; }
  public string? ClientIp { get; set; }
  public DateTime? ExpireTime { get; set; }
  public string? ChannelOrderNo { get; set; }
}

public class PaymentProviderRefundContext
{
  public string RefundNo { get; set; } = null!;
  public string OrderNo { get; set; } = null!;
  public long Amount { get; set; }
  public long TotalAmount { get; set; }
  public string? Reason { get; set; }
  public string? ChannelOrderNo { get; set; }
  public string? ChannelRefundNo { get; set; }
}

public class NativePayResult
{
  public bool Success { get; set; }
  public string? QrCodeUrl { get; set; }
  public string? ChannelOrderNo { get; set; }
  public string? ErrorCode { get; set; }
  public string? ErrorMessage { get; set; }
}

public class QueryPayResult
{
  public PaymentOrderStatus Status { get; set; }
  public string? ChannelOrderNo { get; set; }
  public DateTime? PaidTime { get; set; }
  public string? ErrorMessage { get; set; }
}

public class RefundProviderResult
{
  public bool Success { get; set; }
  public PaymentRefundStatus Status { get; set; }
  public string? ChannelRefundNo { get; set; }
  public DateTime? RefundTime { get; set; }
  public string? ErrorCode { get; set; }
  public string? ErrorMessage { get; set; }
}

public class NotifyHandleResult
{
  public bool Success { get; set; }
  public string? OrderNo { get; set; }
  public string? RefundNo { get; set; }
  public string ResponseBody { get; set; } = "success";
  public string? ProcessMessage { get; set; }
}

public class PaymentNotifyContext
{
  public string RawBody { get; set; } = string.Empty;
  public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

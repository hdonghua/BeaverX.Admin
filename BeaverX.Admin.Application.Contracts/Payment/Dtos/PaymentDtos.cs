using BeaverX.Admin.Domain.Shared.Payment;

namespace BeaverX.Admin.Application.Contracts.Payment.Dtos;

public class PaymentChannelDto
{
  public long Id { get; set; }
  public string ChannelCode { get; set; } = null!;
  public string ChannelName { get; set; } = null!;
  public PaymentProviderType ProviderType { get; set; }
  public bool IsEnabled { get; set; }
  public string ConfigJson { get; set; } = "{}";
  public string? NotifyUrl { get; set; }
  public string? Remark { get; set; }
  public int Sort { get; set; }
  public DateTime CreationTime { get; set; }
}

public class CreatePaymentChannelDto
{
  public string ChannelCode { get; set; } = null!;
  public string ChannelName { get; set; } = null!;
  public PaymentProviderType ProviderType { get; set; }
  public bool IsEnabled { get; set; } = true;
  public string ConfigJson { get; set; } = "{}";
  public string? NotifyUrl { get; set; }
  public string? Remark { get; set; }
  public int Sort { get; set; }
}

public class UpdatePaymentChannelDto
{
  public string? ChannelName { get; set; }
  public bool? IsEnabled { get; set; }
  public string? ConfigJson { get; set; }
  public string? NotifyUrl { get; set; }
  public string? Remark { get; set; }
  public int? Sort { get; set; }
}

public class PaymentChannelQueryDto
{
  public int Page { get; set; } = 1;
  public int PageSize { get; set; } = 20;
  public string? Keyword { get; set; }
  public bool? IsEnabled { get; set; }
}

public class PaymentOrderDto
{
  public long Id { get; set; }
  public string OrderNo { get; set; } = null!;
  public string ChannelCode { get; set; } = null!;
  public string Subject { get; set; } = null!;
  public string? Description { get; set; }
  public long Amount { get; set; }
  public string Currency { get; set; } = "CNY";
  public PaymentOrderStatus Status { get; set; }
  public string? Attach { get; set; }
  public string? BusinessType { get; set; }
  public string? BusinessId { get; set; }
  public long? UserId { get; set; }
  public DateTime? ExpireTime { get; set; }
  public DateTime? PaidTime { get; set; }
  public string? ChannelOrderNo { get; set; }
  public string? QrCodeUrl { get; set; }
  public long RefundedAmount { get; set; }
  public string? ErrorMessage { get; set; }
  public DateTime CreationTime { get; set; }
}

public class CreatePaymentOrderDto
{
  public string ChannelCode { get; set; } = null!;
  public string Subject { get; set; } = null!;
  public string? Description { get; set; }
  /// <summary>金额（分）</summary>
  public long Amount { get; set; }
  public string? Attach { get; set; }
  public string? BusinessType { get; set; }
  public string? BusinessId { get; set; }
  public int? ExpireMinutes { get; set; }
}

public class PaymentOrderQueryDto
{
  public int Page { get; set; } = 1;
  public int PageSize { get; set; } = 20;
  public string? OrderNo { get; set; }
  public string? ChannelCode { get; set; }
  public PaymentOrderStatus? Status { get; set; }
  public DateTime? StartTime { get; set; }
  public DateTime? EndTime { get; set; }
}

public class CreatePaymentOrderResultDto
{
  public PaymentOrderDto Order { get; set; } = null!;
  public string QrCodeUrl { get; set; } = null!;
}

public class CreatePaymentRefundDto
{
  public long PaymentOrderId { get; set; }
  /// <summary>退款金额（分），不传则全额退</summary>
  public long? Amount { get; set; }
  public string? Reason { get; set; }
}

public class PaymentRefundDto
{
  public long Id { get; set; }
  public string RefundNo { get; set; } = null!;
  public long PaymentOrderId { get; set; }
  public string OrderNo { get; set; } = null!;
  public string ChannelCode { get; set; } = null!;
  public long Amount { get; set; }
  public long TotalAmount { get; set; }
  public PaymentRefundStatus Status { get; set; }
  public string? ChannelRefundNo { get; set; }
  public string? Reason { get; set; }
  public DateTime? RefundTime { get; set; }
  public string? ErrorMessage { get; set; }
  public DateTime CreationTime { get; set; }
}

public class PaymentRefundQueryDto
{
  public int Page { get; set; } = 1;
  public int PageSize { get; set; } = 20;
  public string? OrderNo { get; set; }
  public string? RefundNo { get; set; }
  public PaymentRefundStatus? Status { get; set; }
}

public class PaymentNotifyLogDto
{
  public long Id { get; set; }
  public string NotifyType { get; set; } = null!;
  public string ChannelCode { get; set; } = null!;
  public string? OrderNo { get; set; }
  public string? RefundNo { get; set; }
  public bool ProcessSuccess { get; set; }
  public string? ProcessMessage { get; set; }
  public DateTime CreatedTime { get; set; }
}

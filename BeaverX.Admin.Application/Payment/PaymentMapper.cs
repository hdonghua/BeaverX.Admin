using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Application.Contracts.Payment.Dtos;
using BeaverX.Admin.Domain.Payment;

namespace BeaverX.Admin.Application.Payment;

internal static class PaymentMapper
{
  public static PaymentChannelDto ToChannelDto(PaymentChannel entity) => new()
  {
    Id = entity.Id,
    ChannelCode = entity.ChannelCode,
    ChannelName = entity.ChannelName,
    ProviderType = entity.ProviderType,
    IsEnabled = entity.IsEnabled,
    ConfigJson = entity.ConfigJson,
    NotifyUrl = entity.NotifyUrl,
    Remark = entity.Remark,
    Sort = entity.Sort,
    CreationTime = entity.CreationTime,
  };

  public static PaymentOrderDto ToOrderDto(PaymentOrder entity) => new()
  {
    Id = entity.Id,
    OrderNo = entity.OrderNo,
    ChannelCode = entity.ChannelCode,
    Subject = entity.Subject,
    Description = entity.Description,
    Amount = entity.Amount,
    Currency = entity.Currency,
    Status = entity.Status,
    Attach = entity.Attach,
    BusinessType = entity.BusinessType,
    BusinessId = entity.BusinessId,
    UserId = entity.UserId,
    ExpireTime = entity.ExpireTime,
    PaidTime = entity.PaidTime,
    ChannelOrderNo = entity.ChannelOrderNo,
    QrCodeUrl = entity.QrCodeUrl,
    AppPayOrderString = entity.AppPayOrderString,
    RefundedAmount = entity.RefundedAmount,
    ErrorMessage = entity.ErrorMessage,
    CreationTime = entity.CreationTime,
  };

  public static PaymentRefundDto ToRefundDto(PaymentRefund entity) => new()
  {
    Id = entity.Id,
    RefundNo = entity.RefundNo,
    PaymentOrderId = entity.PaymentOrderId,
    OrderNo = entity.OrderNo,
    ChannelCode = entity.ChannelCode,
    Amount = entity.Amount,
    TotalAmount = entity.TotalAmount,
    Status = entity.Status,
    ChannelRefundNo = entity.ChannelRefundNo,
    Reason = entity.Reason,
    RefundTime = entity.RefundTime,
    ErrorMessage = entity.ErrorMessage,
    CreationTime = entity.CreationTime,
  };

  public static PaymentProviderChannelContext ToProviderChannel(
    PaymentChannel entity,
    string? configJson = null) => new()
  {
    ChannelId = entity.Id,
    ChannelCode = entity.ChannelCode,
    ProviderType = entity.ProviderType,
    ConfigJson = configJson ?? entity.ConfigJson,
  };

  public static PaymentProviderOrderContext ToProviderOrder(PaymentOrder entity) => new()
  {
    OrderNo = entity.OrderNo,
    Subject = entity.Subject,
    Description = entity.Description,
    Amount = entity.Amount,
    Currency = entity.Currency,
    Attach = entity.Attach,
    ClientIp = entity.ClientIp,
    ExpireTime = entity.ExpireTime,
    ChannelOrderNo = entity.ChannelOrderNo,
  };

  public static PaymentProviderRefundContext ToProviderRefund(PaymentRefund entity) => new()
  {
    RefundNo = entity.RefundNo,
    OrderNo = entity.OrderNo,
    Amount = entity.Amount,
    TotalAmount = entity.TotalAmount,
    Reason = entity.Reason,
    ChannelOrderNo = entity.ChannelOrderNo,
    ChannelRefundNo = entity.ChannelRefundNo,
  };
}

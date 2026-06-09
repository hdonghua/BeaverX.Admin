using System.Text.Json;
using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Core.Dependency;

namespace BeaverX.Admin.Infrastructure.Payment;

/// <summary>
/// 沙箱原生扫码支付，用于本地联调（配合模拟支付接口）
/// </summary>
public class SandboxNativePaymentProvider : IPaymentProvider, IScopedDependency
{
  public string ChannelCode => PaymentChannelCodes.SandboxNative;

  public Task<NativePayResult> CreateNativePayAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    string notifyUrl,
    CancellationToken cancellationToken = default)
  {
    var qrCodeUrl = $"sandbox://beaverx/pay/{order.OrderNo}";
    return Task.FromResult(new NativePayResult
    {
      Success = true,
      QrCodeUrl = qrCodeUrl,
      ChannelOrderNo = $"SANDBOX{order.OrderNo}",
    });
  }

  public Task<QueryPayResult> QueryPayAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult(new QueryPayResult
    {
      Status = PaymentOrderStatus.Paying,
    });
  }

  public Task<RefundProviderResult> RefundAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    PaymentProviderRefundContext refund,
    string notifyUrl,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult(new RefundProviderResult
    {
      Success = true,
      Status = PaymentRefundStatus.Success,
      ChannelRefundNo = $"SANDBOX_RF_{refund.RefundNo}",
      RefundTime = DateTime.UtcNow,
    });
  }

  public Task<NotifyHandleResult> HandlePayNotifyAsync(
    PaymentProviderChannelContext channel,
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default)
  {
    try
    {
      using var doc = JsonDocument.Parse(context.RawBody);
      var orderNo = doc.RootElement.GetProperty("orderNo").GetString();
      return Task.FromResult(new NotifyHandleResult
      {
        Success = !string.IsNullOrWhiteSpace(orderNo),
        OrderNo = orderNo,
        ResponseBody = "success",
        ProcessMessage = "sandbox pay notify",
      });
    }
    catch (Exception ex)
    {
      return Task.FromResult(new NotifyHandleResult
      {
        Success = false,
        ResponseBody = "fail",
        ProcessMessage = ex.Message,
      });
    }
  }

  public Task<NotifyHandleResult> HandleRefundNotifyAsync(
    PaymentProviderChannelContext channel,
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default)
  {
    try
    {
      using var doc = JsonDocument.Parse(context.RawBody);
      var refundNo = doc.RootElement.GetProperty("refundNo").GetString();
      return Task.FromResult(new NotifyHandleResult
      {
        Success = !string.IsNullOrWhiteSpace(refundNo),
        RefundNo = refundNo,
        ResponseBody = "success",
        ProcessMessage = "sandbox refund notify",
      });
    }
    catch (Exception ex)
    {
      return Task.FromResult(new NotifyHandleResult
      {
        Success = false,
        ResponseBody = "fail",
        ProcessMessage = ex.Message,
      });
    }
  }
}

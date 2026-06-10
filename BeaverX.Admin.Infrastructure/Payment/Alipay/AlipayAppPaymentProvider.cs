using Aop.Api.Domain;
using Aop.Api.Request;
using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Core.Dependency;

namespace BeaverX.Admin.Infrastructure.Payment.Alipay;

/// <summary>
/// 支付宝 App 支付 Provider（alipay.trade.app.pay，product_code: QUICK_MSECURITY_PAY）。
/// </summary>
public class AlipayAppPaymentProvider : AlipayPaymentProviderBase, IScopedDependency
{
  public override string ChannelCode => PaymentChannelCodes.AlipayAppPay;

  public override Task<AppPayResult> CreateAppPayAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    string notifyUrl,
    CancellationToken cancellationToken = default)
  {
    var config = PaymentConfigHelper.ParseConfig<AlipayChannelConfig>(channel.ConfigJson);
    if (!AlipayPayHelper.ValidateConfig(config, out var error))
    {
      return Task.FromResult(AppPayResult.Fail("CONFIG", error));
    }

    try
    {
      var client = AlipaySdkClientFactory.Create(config);
      var request = new AlipayTradeAppPayRequest();
      request.SetNotifyUrl(notifyUrl);
      request.SetBizModel(new AlipayTradeAppPayModel
      {
        OutTradeNo = order.OrderNo,
        TotalAmount = AlipayPayHelper.FormatAmountYuan(order.Amount),
        Subject = order.Subject,
        Body = order.Description,
        ProductCode = "QUICK_MSECURITY_PAY",
        TimeoutExpress = "30m",
      });

      var response = client.SdkExecute(request);
      if (response.IsError)
      {
        return Task.FromResult(AppPayResult.Fail(
          response.SubCode ?? response.Code,
          response.SubMsg ?? response.Msg));
      }

      if (string.IsNullOrWhiteSpace(response.Body))
      {
        return Task.FromResult(AppPayResult.Fail("NO_ORDER_STRING", "支付宝未返回 App 支付参数"));
      }

      return Task.FromResult(new AppPayResult
      {
        Success = true,
        AppPayOrderString = response.Body,
        ChannelOrderNo = order.OrderNo,
      });
    }
    catch (Exception ex)
    {
      return Task.FromResult(AppPayResult.Fail("SDK", ex.Message));
    }
  }
}

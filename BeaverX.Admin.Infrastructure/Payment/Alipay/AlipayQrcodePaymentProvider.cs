using Aop.Api.Domain;
using Aop.Api.Request;
using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Core.Dependency;

namespace BeaverX.Admin.Infrastructure.Payment.Alipay;

/// <summary>
/// 支付宝二维码支付 Provider（alipay.trade.precreate，product_code: QR_CODE_OFFLINE）。
/// </summary>
public class AlipayQrcodePaymentProvider : AlipayPaymentProviderBase, IScopedDependency
{
  public override string ChannelCode => PaymentChannelCodes.AlipayQrcode;

  public override Task<QrcodePayResult> CreateQrcodePayAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    string notifyUrl,
    CancellationToken cancellationToken = default)
  {
    var config = PaymentConfigHelper.ParseConfig<AlipayChannelConfig>(channel.ConfigJson);
    if (!AlipayPayHelper.ValidateConfig(config, out var error))
    {
      return Task.FromResult(QrcodePayResult.Fail("CONFIG", error));
    }

    try
    {
      var client = AlipaySdkClientFactory.Create(config);
      var request = new AlipayTradePrecreateRequest();
      request.SetNotifyUrl(notifyUrl);
      request.SetBizModel(new AlipayTradePrecreateModel
      {
        OutTradeNo = order.OrderNo,
        TotalAmount = AlipayPayHelper.FormatAmountYuan(order.Amount),
        Subject = order.Subject,
        Body = order.Description,
        ProductCode = "QR_CODE_OFFLINE",
        TimeoutExpress = "30m",
      });

      var certMode = AlipaySdkClientFactory.UsesCertificateMode(config);
      var response = certMode
        ? client.CertificateExecute(request)
        : client.Execute(request);
      if (response.IsError)
      {
        return Task.FromResult(QrcodePayResult.Fail(
          response.SubCode ?? response.Code,
          response.SubMsg ?? response.Msg));
      }

      if (string.IsNullOrWhiteSpace(response.QrCode))
      {
        return Task.FromResult(QrcodePayResult.Fail("NO_QR", "支付宝未返回二维码"));
      }

      return Task.FromResult(new QrcodePayResult
      {
        Success = true,
        QrCodeUrl = response.QrCode,
        ChannelOrderNo = order.OrderNo,
      });
    }
    catch (Exception ex)
    {
      return Task.FromResult(QrcodePayResult.Fail("SDK", ex.Message));
    }
  }
}

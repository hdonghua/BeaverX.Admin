using System.Globalization;
using Aop.Api;
using Aop.Api.Domain;
using Aop.Api.Request;
using Aop.Api.Response;
using Aop.Api.Util;
using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Core.Dependency;

namespace BeaverX.Admin.Infrastructure.Payment.Alipay;

public class AlipayNativePaymentProvider : IPaymentProvider, IScopedDependency
{
  public string ChannelCode => PaymentChannelCodes.AlipayNative;

  public Task<NativePayResult> CreateNativePayAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    string notifyUrl,
    CancellationToken cancellationToken = default)
  {
    var config = PaymentConfigHelper.ParseConfig<AlipayChannelConfig>(channel.ConfigJson);
    if (!ValidateConfig(config, out var error))
    {
      return Task.FromResult(FailNative("CONFIG", error));
    }

    try
    {
      var client = AlipaySdkClientFactory.Create(config);
      var request = new AlipayTradePrecreateRequest();
      request.SetNotifyUrl(notifyUrl);
      request.SetBizModel(new AlipayTradePrecreateModel
      {
        OutTradeNo = order.OrderNo,
        TotalAmount = FormatAmountYuan(order.Amount),
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
        return Task.FromResult(FailNative(response.SubCode ?? response.Code, response.SubMsg ?? response.Msg));
      }

      if (string.IsNullOrWhiteSpace(response.QrCode))
      {
        return Task.FromResult(FailNative("NO_QR", "支付宝未返回二维码"));
      }

      return Task.FromResult(new NativePayResult
      {
        Success = true,
        QrCodeUrl = response.QrCode,
        ChannelOrderNo = order.OrderNo,
      });
    }
    catch (Exception ex)
    {
      return Task.FromResult(FailNative("SDK", ex.Message));
    }
  }

  public Task<QueryPayResult> QueryPayAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    CancellationToken cancellationToken = default)
  {
    var config = PaymentConfigHelper.ParseConfig<AlipayChannelConfig>(channel.ConfigJson);
    if (!ValidateConfig(config, out var error))
    {
      return Task.FromResult(new QueryPayResult
      {
        Status = PaymentOrderStatus.Paying,
        ErrorMessage = error,
      });
    }

    try
    {
      var client = AlipaySdkClientFactory.Create(config);
      var request = new AlipayTradeQueryRequest();
      request.SetBizModel(new AlipayTradeQueryModel
      {
        OutTradeNo = order.OrderNo,
      });

      var certMode = AlipaySdkClientFactory.UsesCertificateMode(config);
      var response = certMode
        ? client.CertificateExecute(request)
        : client.Execute(request);
      if (response.IsError)
      {
        return Task.FromResult(new QueryPayResult
        {
          Status = PaymentOrderStatus.Paying,
          ErrorMessage = response.SubMsg ?? response.Msg,
        });
      }

      var status = response.TradeStatus switch
      {
        "TRADE_SUCCESS" or "TRADE_FINISHED" => PaymentOrderStatus.Success,
        "TRADE_CLOSED" => PaymentOrderStatus.Closed,
        _ => PaymentOrderStatus.Paying,
      };

      DateTime? paidTime = null;
      if (!string.IsNullOrWhiteSpace(response.SendPayDate) &&
          DateTime.TryParse(response.SendPayDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
      {
        paidTime = parsed;
      }

      return Task.FromResult(new QueryPayResult
      {
        Status = status,
        ChannelOrderNo = response.TradeNo,
        PaidTime = paidTime,
      });
    }
    catch (Exception ex)
    {
      return Task.FromResult(new QueryPayResult
      {
        Status = PaymentOrderStatus.Paying,
        ErrorMessage = ex.Message,
      });
    }
  }

  public Task<RefundProviderResult> RefundAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    PaymentProviderRefundContext refund,
    string notifyUrl,
    CancellationToken cancellationToken = default)
  {
    var config = PaymentConfigHelper.ParseConfig<AlipayChannelConfig>(channel.ConfigJson);
    if (!ValidateConfig(config, out var error))
    {
      return Task.FromResult(new RefundProviderResult
      {
        Success = false,
        Status = PaymentRefundStatus.Failed,
        ErrorMessage = error,
      });
    }

    try
    {
      var client = AlipaySdkClientFactory.Create(config);
      var request = new AlipayTradeRefundRequest();
      request.SetNotifyUrl(notifyUrl);
      request.SetBizModel(new AlipayTradeRefundModel
      {
        OutTradeNo = order.OrderNo,
        RefundAmount = FormatAmountYuan(refund.Amount),
        OutRequestNo = refund.RefundNo,
        RefundReason = refund.Reason,
      });

      var certMode = AlipaySdkClientFactory.UsesCertificateMode(config);
      var response = certMode
        ? client.CertificateExecute(request)
        : client.Execute(request);
      if (response.IsError)
      {
        return Task.FromResult(new RefundProviderResult
        {
          Success = false,
          Status = PaymentRefundStatus.Failed,
          ErrorCode = response.SubCode ?? response.Code,
          ErrorMessage = response.SubMsg ?? response.Msg,
        });
      }

      return Task.FromResult(new RefundProviderResult
      {
        Success = true,
        Status = PaymentRefundStatus.Success,
        ChannelRefundNo = response.TradeNo ?? refund.RefundNo,
        RefundTime = DateTime.UtcNow,
      });
    }
    catch (Exception ex)
    {
      return Task.FromResult(new RefundProviderResult
      {
        Success = false,
        Status = PaymentRefundStatus.Failed,
        ErrorMessage = ex.Message,
      });
    }
  }

  public Task<NotifyHandleResult> HandlePayNotifyAsync(
    PaymentProviderChannelContext channel,
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default)
  {
    var config = PaymentConfigHelper.ParseConfig<AlipayChannelConfig>(channel.ConfigJson);
    var parameters = ParseNotifyParameters(context.RawBody);
    if (!VerifyNotify(config, parameters, out var error))
    {
      return Task.FromResult(new NotifyHandleResult
      {
        Success = false,
        ResponseBody = "fail",
        ProcessMessage = error,
      });
    }

    var tradeStatus = parameters.GetValueOrDefault("trade_status");
    if (tradeStatus is not "TRADE_SUCCESS" and not "TRADE_FINISHED")
    {
      return Task.FromResult(new NotifyHandleResult
      {
        Success = true,
        ResponseBody = "success",
        ProcessMessage = $"忽略状态: {tradeStatus}",
      });
    }

    return Task.FromResult(new NotifyHandleResult
    {
      Success = true,
      OrderNo = parameters.GetValueOrDefault("out_trade_no"),
      ResponseBody = "success",
      ProcessMessage = "alipay pay notify",
    });
  }

  public Task<NotifyHandleResult> HandleRefundNotifyAsync(
    PaymentProviderChannelContext channel,
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default)
  {
    var config = PaymentConfigHelper.ParseConfig<AlipayChannelConfig>(channel.ConfigJson);
    var parameters = ParseNotifyParameters(context.RawBody);
    if (!VerifyNotify(config, parameters, out var error))
    {
      return Task.FromResult(new NotifyHandleResult
      {
        Success = false,
        ResponseBody = "fail",
        ProcessMessage = error,
      });
    }

    return Task.FromResult(new NotifyHandleResult
    {
      Success = true,
      RefundNo = parameters.GetValueOrDefault("out_biz_no") ?? parameters.GetValueOrDefault("out_request_no"),
      ResponseBody = "success",
      ProcessMessage = "alipay refund notify",
    });
  }

  private static bool VerifyNotify(
    AlipayChannelConfig config,
    Dictionary<string, string> parameters,
    out string error)
  {
    if (parameters.Count == 0)
    {
      error = "支付宝回调参数为空";
      return false;
    }

    var signType = string.IsNullOrWhiteSpace(config.SignType) ? "RSA2" : config.SignType;
    try
    {
      if (AlipaySdkClientFactory.UsesCertificateMode(config))
      {
        var verified = AlipaySignature.RSACertCheckV1(
          parameters,
          config.AlipayPublicCertPath!,
          "utf-8",
          signType);
        if (!verified)
        {
          error = "支付宝证书模式回调验签失败";
          return false;
        }
      }
      else
      {
        var verified = AlipaySignature.RSACheckV1(
          parameters,
          config.AlipayPublicKey,
          "utf-8",
          signType,
          false);
        if (!verified)
        {
          error = "支付宝公钥模式回调验签失败";
          return false;
        }
      }

      error = string.Empty;
      return true;
    }
    catch (Exception ex)
    {
      error = ex.Message;
      return false;
    }
  }

  private static bool ValidateConfig(AlipayChannelConfig config, out string error)
  {
    if (string.IsNullOrWhiteSpace(config.AppId) ||
        string.IsNullOrWhiteSpace(config.PrivateKey))
    {
      error = "支付宝 AppId 或应用私钥未配置";
      return false;
    }

    if (AlipaySdkClientFactory.UsesCertificateMode(config))
    {
      error = string.Empty;
      return true;
    }

    if (string.IsNullOrWhiteSpace(config.AlipayPublicKey))
    {
      error = "支付宝公钥未配置（公钥模式需填写 alipayPublicKey）";
      return false;
    }

    error = string.Empty;
    return true;
  }

  private static Dictionary<string, string> ParseNotifyParameters(string rawBody)
  {
    var result = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var pair in rawBody.Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
      var parts = pair.Split('=', 2);
      if (parts.Length == 2)
      {
        result[parts[0]] = Uri.UnescapeDataString(parts[1].Replace('+', ' '));
      }
    }

    return result;
  }

  private static string FormatAmountYuan(long amountCents) =>
    (amountCents / 100m).ToString("0.##", CultureInfo.InvariantCulture);

  private static NativePayResult FailNative(string? code, string? message) => new()
  {
    Success = false,
    ErrorCode = code,
    ErrorMessage = message,
  };
}

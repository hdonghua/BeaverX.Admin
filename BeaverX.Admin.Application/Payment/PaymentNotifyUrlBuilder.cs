using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Domain.Shared.Payment;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Application.Payment;

/// <summary>根据渠道编码与全局配置拼装支付/退款回调 URL</summary>
public class PaymentNotifyUrlBuilder
{
  private readonly PaymentOptions _options;

  public PaymentNotifyUrlBuilder(IOptions<PaymentOptions> options)
  {
    _options = options.Value;
  }

  /// <summary>支付成功异步通知地址</summary>
  public string BuildPayNotifyUrl(string channelCode, string? channelOverride)
  {
    if (!string.IsNullOrWhiteSpace(channelOverride))
    {
      return channelOverride.Trim();
    }

    var baseUrl = _options.BaseNotifyUrl.TrimEnd('/');
    return $"{baseUrl}/api/PaymentNotify/{GetNotifySegment(channelCode)}/pay";
  }

  /// <summary>退款结果异步通知地址</summary>
  public string BuildRefundNotifyUrl(string channelCode, string? channelOverride)
  {
    if (!string.IsNullOrWhiteSpace(channelOverride))
    {
      return channelOverride.Trim();
    }

    var baseUrl = _options.BaseNotifyUrl.TrimEnd('/');
    return $"{baseUrl}/api/PaymentNotify/{GetNotifySegment(channelCode)}/refund";
  }

  private static string GetNotifySegment(string channelCode)
  {
    if (channelCode == PaymentChannelCodes.WeChatQrcode)
    {
      return "wechat";
    }

    if (PaymentChannelCodes.IsAlipay(channelCode))
    {
      return "alipay";
    }

    return channelCode;
  }
}

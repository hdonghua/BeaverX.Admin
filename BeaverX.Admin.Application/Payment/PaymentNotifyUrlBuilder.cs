using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Domain.Shared.Payment;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Application.Payment;

public class PaymentNotifyUrlBuilder
{
  private readonly PaymentOptions _options;

  public PaymentNotifyUrlBuilder(IOptions<PaymentOptions> options)
  {
    _options = options.Value;
  }

  public string BuildPayNotifyUrl(string channelCode, string? channelOverride)
  {
    if (!string.IsNullOrWhiteSpace(channelOverride))
    {
      return channelOverride.Trim();
    }

    var baseUrl = _options.BaseNotifyUrl.TrimEnd('/');
    return $"{baseUrl}/api/PaymentNotify/{GetNotifySegment(channelCode)}/pay";
  }

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
    if (channelCode == PaymentChannelCodes.WeChatNative)
    {
      return "wechat";
    }

    if (channelCode == PaymentChannelCodes.AlipayNative)
    {
      return "alipay";
    }

    return "sandbox";
  }
}

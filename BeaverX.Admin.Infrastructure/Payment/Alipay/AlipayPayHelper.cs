using BeaverX.Admin.Application.Contracts.Payment;

namespace BeaverX.Admin.Infrastructure.Payment.Alipay;

/// <summary>支付宝 Provider 公共辅助方法</summary>
internal static class AlipayPayHelper
{
  internal static bool ValidateConfig(AlipayChannelConfig config, out string error)
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

  internal static string FormatAmountYuan(long amountCents) =>
    (amountCents / 100m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}

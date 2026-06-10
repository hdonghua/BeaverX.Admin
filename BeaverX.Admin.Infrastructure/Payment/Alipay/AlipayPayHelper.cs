using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Domain.Shared.Payment;

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

    if (!AlipayPaymentConstants.IsSupportedSignType(config.SignType))
    {
      error =
        $"签名类型无效，仅支持：{string.Join("、", AlipayPaymentConstants.SupportedSignTypes)}";
      return false;
    }

    if (!AlipayPaymentConstants.IsSupportedGateway(config.Gateway))
    {
      error =
        $"网关地址无效，仅支持：{string.Join("、", AlipayPaymentConstants.SupportedGateways)}";
      return false;
    }

    var certMode = AlipaySdkClientFactory.UsesCertificateMode(config);
    var hasAnyCert =
      !string.IsNullOrWhiteSpace(config.MerchantCertUrl) ||
      !string.IsNullOrWhiteSpace(config.AlipayPublicCertUrl) ||
      !string.IsNullOrWhiteSpace(config.AlipayRootCertUrl) ||
      !string.IsNullOrWhiteSpace(config.MerchantCertPath) ||
      !string.IsNullOrWhiteSpace(config.AlipayPublicCertPath) ||
      !string.IsNullOrWhiteSpace(config.AlipayRootCertPath);

    if (hasAnyCert && !certMode)
    {
      error = "证书模式需上传完整三项证书";
      return false;
    }

    if (certMode)
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

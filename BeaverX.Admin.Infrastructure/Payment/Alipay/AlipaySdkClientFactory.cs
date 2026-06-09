using Aop.Api;

namespace BeaverX.Admin.Infrastructure.Payment.Alipay;

internal static class AlipaySdkClientFactory
{
  public static IAopClient Create(AlipayChannelConfig config)
  {
    var gateway = string.IsNullOrWhiteSpace(config.Gateway)
      ? "https://openapi.alipay.com/gateway.do"
      : config.Gateway.Trim();

    var signType = string.IsNullOrWhiteSpace(config.SignType) ? "RSA2" : config.SignType.Trim();
    var privateKey = NormalizeKey(config.PrivateKey);

    if (UsesCertificateMode(config))
    {
      var certParams = new CertParams
      {
        AppCertPath = config.MerchantCertPath!.Trim(),
        AlipayPublicCertPath = config.AlipayPublicCertPath!.Trim(),
        RootCertPath = config.AlipayRootCertPath!.Trim(),
      };

      return new DefaultAopClient(
        gateway,
        config.AppId.Trim(),
        privateKey,
        "json",
        "1.0",
        signType,
        "utf-8",
        false,
        certParams);
    }

    return new DefaultAopClient(
      gateway,
      config.AppId.Trim(),
      privateKey,
      "json",
      "1.0",
      signType,
      NormalizeKey(config.AlipayPublicKey),
      "utf-8",
      false);
  }

  public static bool UsesCertificateMode(AlipayChannelConfig config) =>
    !string.IsNullOrWhiteSpace(config.MerchantCertPath) &&
    !string.IsNullOrWhiteSpace(config.AlipayPublicCertPath) &&
    !string.IsNullOrWhiteSpace(config.AlipayRootCertPath);

  private static string NormalizeKey(string key)
  {
    if (string.IsNullOrWhiteSpace(key))
    {
      return string.Empty;
    }

    return key.Trim().Replace("\\n", "\n");
  }
}

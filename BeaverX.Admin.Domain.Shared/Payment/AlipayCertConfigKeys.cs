namespace BeaverX.Admin.Domain.Shared.Payment;

/// <summary>支付宝证书在 ConfigJson 中的字段名</summary>
public static class AlipayCertConfigKeys
{
  public const string MerchantCertUrl = "merchantCertUrl";
  public const string MerchantCertPath = "merchantCertPath";
  public const string MerchantCertFileName = "merchantCertFileName";

  public const string AlipayPublicCertUrl = "alipayPublicCertUrl";
  public const string AlipayPublicCertPath = "alipayPublicCertPath";
  public const string AlipayPublicCertFileName = "alipayPublicCertFileName";

  public const string AlipayRootCertUrl = "alipayRootCertUrl";
  public const string AlipayRootCertPath = "alipayRootCertPath";
  public const string AlipayRootCertFileName = "alipayRootCertFileName";

  public static readonly (string Url, string Path, string FileName)[] CertFields =
  [
    (MerchantCertUrl, MerchantCertPath, MerchantCertFileName),
    (AlipayPublicCertUrl, AlipayPublicCertPath, AlipayPublicCertFileName),
    (AlipayRootCertUrl, AlipayRootCertPath, AlipayRootCertFileName),
  ];
}

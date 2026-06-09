namespace BeaverX.Admin.Infrastructure.Payment.Alipay;

internal class AlipayChannelConfig
{
  public string AppId { get; set; } = string.Empty;
  public string PrivateKey { get; set; } = string.Empty;
  public string AlipayPublicKey { get; set; } = string.Empty;
  public string SignType { get; set; } = "RSA2";
  public string Gateway { get; set; } = "https://openapi.alipay.com/gateway.do";
  /// <summary>公钥证书模式：应用公钥证书路径</summary>
  public string? MerchantCertPath { get; set; }
  /// <summary>公钥证书模式：支付宝公钥证书路径</summary>
  public string? AlipayPublicCertPath { get; set; }
  /// <summary>公钥证书模式：支付宝根证书路径</summary>
  public string? AlipayRootCertPath { get; set; }
}

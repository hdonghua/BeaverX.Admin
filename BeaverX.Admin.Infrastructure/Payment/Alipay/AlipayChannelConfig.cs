namespace BeaverX.Admin.Infrastructure.Payment.Alipay;

/// <summary>支付宝渠道 JSON 配置模型（二维码 / App 共用）</summary>
internal class AlipayChannelConfig
{
  public string AppId { get; set; } = string.Empty;

  /// <summary>应用私钥（RSA2）</summary>
  public string PrivateKey { get; set; } = string.Empty;

  /// <summary>公钥模式：支付宝公钥</summary>
  public string AlipayPublicKey { get; set; } = string.Empty;
  public string SignType { get; set; } = "RSA2";
  public string Gateway { get; set; } = "https://openapi.alipay.com/gateway.do";

  /// <summary>证书模式：应用公钥证书路径</summary>
  public string? MerchantCertPath { get; set; }

  /// <summary>证书模式：支付宝公钥证书路径</summary>
  public string? AlipayPublicCertPath { get; set; }

  /// <summary>证书模式：支付宝根证书路径</summary>
  public string? AlipayRootCertPath { get; set; }
}

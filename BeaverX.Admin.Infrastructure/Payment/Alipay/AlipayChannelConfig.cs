namespace BeaverX.Admin.Infrastructure.Payment.Alipay;

/// <summary>支付宝渠道 JSON 配置模型（二维码 / App 共用）</summary>
internal class AlipayChannelConfig
{
  public string AppId { get; set; } = string.Empty;
  public string PrivateKey { get; set; } = string.Empty;
  public string AlipayPublicKey { get; set; } = string.Empty;
  public string SignType { get; set; } = "RSA2";
  public string Gateway { get; set; } = "https://openapi.alipay.com/gateway.do";

  /// <summary>证书模式：应用公钥证书下载地址（/api/File/proxy/...）</summary>
  public string? MerchantCertUrl { get; set; }

  /// <summary>证书模式：应用公钥证书本地相对路径，如 cert/appCert_1.crt</summary>
  public string? MerchantCertPath { get; set; }

  public string? MerchantCertFileName { get; set; }

  public string? AlipayPublicCertUrl { get; set; }
  public string? AlipayPublicCertPath { get; set; }
  public string? AlipayPublicCertFileName { get; set; }

  public string? AlipayRootCertUrl { get; set; }
  public string? AlipayRootCertPath { get; set; }
  public string? AlipayRootCertFileName { get; set; }
}

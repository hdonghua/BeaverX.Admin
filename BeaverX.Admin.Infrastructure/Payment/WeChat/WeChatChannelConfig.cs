namespace BeaverX.Admin.Infrastructure.Payment.WeChat;

/// <summary>微信支付渠道 JSON 配置模型（二维码支付）</summary>
internal class WeChatChannelConfig
{
  public string AppId { get; set; } = string.Empty;

  /// <summary>微信支付商户号</summary>
  public string MchId { get; set; } = string.Empty;

  /// <summary>APIv3 密钥（32 位，用于回调解密）</summary>
  public string ApiV3Key { get; set; } = string.Empty;

  /// <summary>商户 API 证书序列号</summary>
  public string CertSerialNo { get; set; } = string.Empty;

  /// <summary>商户 API 私钥 PEM</summary>
  public string PrivateKey { get; set; } = string.Empty;

  /// <summary>微信平台证书 PEM（用于回调验签）</summary>
  public string? PlatformCert { get; set; }
}

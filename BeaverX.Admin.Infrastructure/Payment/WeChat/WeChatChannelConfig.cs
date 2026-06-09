namespace BeaverX.Admin.Infrastructure.Payment.WeChat;

internal class WeChatChannelConfig
{
  public string AppId { get; set; } = string.Empty;
  public string MchId { get; set; } = string.Empty;
  public string ApiV3Key { get; set; } = string.Empty;
  public string CertSerialNo { get; set; } = string.Empty;
  public string PrivateKey { get; set; } = string.Empty;
  public string? PlatformCert { get; set; }
}

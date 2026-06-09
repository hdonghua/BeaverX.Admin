using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Payment;

/// <summary>
/// 支付渠道配置（密钥、证书等存于 ConfigJson）
/// </summary>
public class PaymentChannel : FullAuditedEntity
{
  public string ChannelCode { get; set; } = null!;
  public string ChannelName { get; set; } = null!;
  public PaymentProviderType ProviderType { get; set; }
  public bool IsEnabled { get; set; } = true;
  /// <summary>JSON 配置：AppId、商户号、API 密钥、证书内容等</summary>
  public string ConfigJson { get; set; } = "{}";
  public string? NotifyUrl { get; set; }
  public string? Remark { get; set; }
  public int Sort { get; set; }
}

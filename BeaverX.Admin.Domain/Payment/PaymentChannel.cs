using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Payment;

/// <summary>支付渠道配置（密钥、证书等存于 <see cref="ConfigJson"/>）</summary>
public class PaymentChannel : FullAuditedEntity
{
  /// <summary>渠道编码，见 <see cref="PaymentChannelCodes"/></summary>
  public string ChannelCode { get; set; } = null!;

  /// <summary>渠道显示名称</summary>
  public string ChannelName { get; set; } = null!;

  /// <summary>支付提供商类型</summary>
  public PaymentProviderType ProviderType { get; set; }

  /// <summary>是否启用</summary>
  public bool IsEnabled { get; set; } = true;

  /// <summary>JSON 配置：AppId、商户号、API 密钥、证书路径或内容等</summary>
  public string ConfigJson { get; set; } = "{}";

  /// <summary>可选：覆盖系统默认的支付/退款回调地址</summary>
  public string? NotifyUrl { get; set; }

  public string? Remark { get; set; }
  public int Sort { get; set; }
}

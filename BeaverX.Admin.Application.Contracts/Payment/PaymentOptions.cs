namespace BeaverX.Admin.Application.Contracts.Payment;

/// <summary>支付模块全局配置（appsettings Payment 节点）</summary>
public class PaymentOptions
{
  public const string SectionName = "Payment";

  /// <summary>回调基础地址，如 https://api.example.com</summary>
  public string BaseNotifyUrl { get; set; } = string.Empty;

  /// <summary>默认订单过期分钟数</summary>
  public int DefaultExpireMinutes { get; set; } = 30;
}

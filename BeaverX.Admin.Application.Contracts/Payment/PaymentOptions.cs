namespace BeaverX.Admin.Application.Contracts.Payment;

public class PaymentOptions
{
  public const string SectionName = "Payment";

  /// <summary>回调基础地址，如 https://api.example.com</summary>
  public string BaseNotifyUrl { get; set; } = string.Empty;

  /// <summary>默认订单过期分钟数</summary>
  public int DefaultExpireMinutes { get; set; } = 30;
}

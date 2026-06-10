namespace BeaverX.Admin.Domain.Shared.Payment;

/// <summary>支付宝渠道配置常量（前后端校验需保持一致）</summary>
public static class AlipayPaymentConstants
{
  /// <summary>
  /// 支持的签名类型。
  /// <list type="bullet">
  ///   <item><description>RSA2 — SHA256WithRSA，支付宝推荐，新应用请使用此项</description></item>
  ///   <item><description>RSA — SHA1WithRSA，旧版兼容，不推荐新项目使用</description></item>
  /// </list>
  /// </summary>
  public static readonly string[] SupportedSignTypes = ["RSA2", "RSA"];

  public const string DefaultSignType = "RSA2";

  /// <summary>
  /// 支持的 OpenAPI 网关地址。
  /// <list type="bullet">
  ///   <item><description>生产环境 — 正式交易</description></item>
  ///   <item><description>沙箱环境 — 联调测试</description></item>
  /// </list>
  /// </summary>
  public static readonly string[] SupportedGateways =
  [
    "https://openapi.alipay.com/gateway.do",
    "https://openapi-sandbox.dl.alipaydev.com/gateway.do",
  ];

  public const string DefaultGateway = "https://openapi.alipay.com/gateway.do";

  public static bool IsSupportedSignType(string? signType)
  {
    if (string.IsNullOrWhiteSpace(signType))
    {
      return true;
    }

    return SupportedSignTypes.Contains(signType.Trim(), StringComparer.OrdinalIgnoreCase);
  }

  public static bool IsSupportedGateway(string? gateway)
  {
    if (string.IsNullOrWhiteSpace(gateway))
    {
      return true;
    }

    var normalized = gateway.Trim();
    return SupportedGateways.Contains(normalized, StringComparer.OrdinalIgnoreCase);
  }

  public static string NormalizeSignType(string? signType) =>
    string.IsNullOrWhiteSpace(signType)
      ? DefaultSignType
      : signType.Trim().ToUpperInvariant();

  public static string NormalizeGateway(string? gateway) =>
    string.IsNullOrWhiteSpace(gateway)
      ? DefaultGateway
      : gateway.Trim();
}

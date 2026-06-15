namespace BeaverX.Admin.Application.Contracts.Payment;

/// <summary>支付模块全局配置（appsettings Payment 节点）</summary>
public class PaymentOptions
{
    public const string SectionName = "Payment";

    /// <summary>回调基础地址，如 https://api.example.com</summary>
    public string BaseNotifyUrl { get; set; } = string.Empty;

    /// <summary>默认订单过期分钟数</summary>
    public int DefaultExpireMinutes { get; set; } = 30;

    /// <summary>
    /// 支付证书本地缓存根目录。
    /// 可为绝对路径，或相对 ContentRoot 的相对路径（启动时解析为绝对路径）。
    /// 落盘路径：{CertCacheRootPath}/{channelId}/merchant_app.crt 等。
    /// </summary>
    public string CertCacheRootPath { get; set; } = "payment-certs";
}

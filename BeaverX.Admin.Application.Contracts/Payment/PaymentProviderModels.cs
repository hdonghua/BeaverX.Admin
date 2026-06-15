using BeaverX.Admin.Domain.Shared.Payment;

namespace BeaverX.Admin.Application.Contracts.Payment;

/// <summary>Provider 调用时的渠道上下文</summary>
public class PaymentProviderChannelContext
{
    /// <summary>渠道编码，见 <see cref="PaymentChannelCodes"/></summary>
    public string ChannelCode { get; set; } = null!;

    /// <summary>渠道主键，用于证书本地缓存目录命名</summary>
    public long ChannelId { get; set; }

    /// <summary>提供商类型</summary>
    public PaymentProviderType ProviderType { get; set; }

    /// <summary>渠道 JSON 配置（密钥、证书等）</summary>
    public string ConfigJson { get; set; } = "{}";
}

/// <summary>Provider 调用时的订单上下文</summary>
public class PaymentProviderOrderContext
{
    public string OrderNo { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>金额（分）</summary>
    public long Amount { get; set; }
    public string Currency { get; set; } = "CNY";
    public string? Attach { get; set; }
    public string? ClientIp { get; set; }
    public DateTime? ExpireTime { get; set; }
    public string? ChannelOrderNo { get; set; }
}

/// <summary>Provider 调用时的退款上下文</summary>
public class PaymentProviderRefundContext
{
    public string RefundNo { get; set; } = null!;
    public string OrderNo { get; set; } = null!;

    /// <summary>退款金额（分）</summary>
    public long Amount { get; set; }

    /// <summary>原订单金额（分）</summary>
    public long TotalAmount { get; set; }
    public string? Reason { get; set; }
    public string? ChannelOrderNo { get; set; }
    public string? ChannelRefundNo { get; set; }
}

/// <summary>二维码支付创建结果（微信/支付宝扫码）</summary>
public class QrcodePayResult
{
    public bool Success { get; set; }

    /// <summary>供用户扫码的链接（微信 code_url / 支付宝 qr_code）</summary>
    public string? QrCodeUrl { get; set; }
    public string? ChannelOrderNo { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static QrcodePayResult Fail(string? code, string? message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message,
    };
}

/// <summary>App 调起支付创建结果（支付宝 App）</summary>
public class AppPayResult
{
    public bool Success { get; set; }

    /// <summary>支付宝 SDK 调起支付用的 orderString</summary>
    public string? AppPayOrderString { get; set; }
    public string? ChannelOrderNo { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static AppPayResult Fail(string? code, string? message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message,
    };
}

/// <summary>渠道侧订单查询结果</summary>
public class QueryPayResult
{
    public PaymentOrderStatus Status { get; set; }
    public string? ChannelOrderNo { get; set; }
    public DateTime? PaidTime { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>渠道侧退款结果</summary>
public class RefundProviderResult
{
    public bool Success { get; set; }
    public PaymentRefundStatus Status { get; set; }
    public string? ChannelRefundNo { get; set; }
    public DateTime? RefundTime { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>异步回调处理结果</summary>
public class NotifyHandleResult
{
    public bool Success { get; set; }
    public string? OrderNo { get; set; }
    public string? RefundNo { get; set; }

    /// <summary>返回给支付平台的响应体</summary>
    public string ResponseBody { get; set; } = "success";
    public string? ProcessMessage { get; set; }
}

/// <summary>支付平台回调 HTTP 上下文</summary>
public class PaymentNotifyContext
{
    public string RawBody { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

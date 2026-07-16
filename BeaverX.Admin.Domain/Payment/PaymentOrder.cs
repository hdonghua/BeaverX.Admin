using BeaverX.Admin.Domain.Shared;
using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Payment;

/// <summary>支付订单（付款单）聚合根。</summary>
public class PaymentOrder : FullAuditedEntity
{
    /// <summary>系统订单号（商户侧 out_trade_no）</summary>
    public string OrderNo { get; private set; } = null!;

    /// <summary>支付渠道编码</summary>
    public string ChannelCode { get; private set; } = null!;

    public string Subject { get; private set; } = null!;
    public string? Description { get; private set; }

    /// <summary>金额（分）</summary>
    public long Amount { get; private set; }
    public string Currency { get; private set; } = "CNY";
    public PaymentOrderStatus Status { get; private set; } = PaymentOrderStatus.Pending;
    public string? ClientIp { get; private set; }

    /// <summary>附加数据，回调时原样返回</summary>
    public string? Attach { get; private set; }
    public string? BusinessType { get; private set; }
    public string? BusinessId { get; private set; }
    public long? UserId { get; private set; }
    public DateTime? ExpireTime { get; private set; }
    public DateTime? PaidTime { get; private set; }

    /// <summary>渠道侧交易号（如微信 transaction_id）</summary>
    public string? ChannelOrderNo { get; private set; }
    public string? ChannelUserId { get; private set; }

    /// <summary>二维码支付链接（微信 code_url / 支付宝 qr_code）</summary>
    public string? QrCodeUrl { get; private set; }

    /// <summary>支付宝 App 支付 orderString</summary>
    public string? AppPayOrderString { get; private set; }

    /// <summary>累计已退款金额（分）</summary>
    public long RefundedAmount { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }

    public PaymentOrder()
    {
    }

    public static PaymentOrder CreatePending(
        string orderNo,
        string channelCode,
        string subject,
        long amount,
        DateTime expireTime,
        string? description = null,
        string? clientIp = null,
        string? attach = null,
        string? businessType = null,
        string? businessId = null,
        long? userId = null)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
        {
            throw new BusinessException("订单号不能为空");
        }

        if (string.IsNullOrWhiteSpace(channelCode))
        {
            throw new BusinessException("支付渠道不能为空");
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new BusinessException("订单标题不能为空");
        }

        if (amount <= 0)
        {
            throw new BusinessException("支付金额必须大于 0");
        }

        return new PaymentOrder
        {
            OrderNo = orderNo.Trim(),
            ChannelCode = channelCode.Trim(),
            Subject = subject.Trim(),
            Description = NormalizeOptional(description),
            Amount = amount,
            Currency = "CNY",
            Status = PaymentOrderStatus.Pending,
            ClientIp = NormalizeOptional(clientIp),
            Attach = NormalizeOptional(attach),
            BusinessType = NormalizeOptional(businessType),
            BusinessId = NormalizeOptional(businessId),
            UserId = userId,
            ExpireTime = expireTime,
        };
    }

    public bool ShouldSkipProviderSync =>
        Status is PaymentOrderStatus.Success
            or PaymentOrderStatus.Refunded
            or PaymentOrderStatus.PartialRefunded;

    public void MarkChannelPayFailed(string? errorCode, string? errorMessage)
    {
        Status = PaymentOrderStatus.Failed;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage ?? "创建支付失败";
    }

    public void MarkPaying(string? qrCodeUrl, string? appPayOrderString, string? channelOrderNo)
    {
        if (Status is not PaymentOrderStatus.Pending and not PaymentOrderStatus.Failed)
        {
            throw new BusinessException($"当前订单状态 {Status} 不允许进入待支付");
        }

        Status = PaymentOrderStatus.Paying;
        QrCodeUrl = qrCodeUrl;
        AppPayOrderString = appPayOrderString;
        ChannelOrderNo = channelOrderNo;
        ErrorCode = null;
        ErrorMessage = null;
    }

    public void ApplyProviderQuery(PaymentOrderStatus providerStatus, DateTime? paidTime, string? channelOrderNo)
    {
        if (providerStatus == PaymentOrderStatus.Success && CanTransitionToPaid())
        {
            Status = PaymentOrderStatus.Success;
            PaidTime = paidTime ?? DateTime.UtcNow;
            ChannelOrderNo = channelOrderNo ?? ChannelOrderNo;
            ErrorCode = null;
            ErrorMessage = null;
            return;
        }

        if (providerStatus == PaymentOrderStatus.Closed &&
            Status is PaymentOrderStatus.Paying or PaymentOrderStatus.Pending)
        {
            Status = PaymentOrderStatus.Closed;
        }
    }

    public bool TryMarkPaidFromNotify()
    {
        if (!CanTransitionToPaid())
        {
            return false;
        }

        Status = PaymentOrderStatus.Success;
        PaidTime = DateTime.UtcNow;
        ErrorCode = null;
        ErrorMessage = null;
        return true;
    }

    public void Close()
    {
        if (Status is PaymentOrderStatus.Success or PaymentOrderStatus.Refunding
            or PaymentOrderStatus.Refunded or PaymentOrderStatus.PartialRefunded)
        {
            throw new BusinessException("当前订单状态不允许关闭");
        }

        Status = PaymentOrderStatus.Closed;
    }

    public long GetRefundableAmount() => Math.Max(0, Amount - RefundedAmount);

    public void EnsureCanRefund(long refundAmount)
    {
        if (Status is not PaymentOrderStatus.Success and not PaymentOrderStatus.PartialRefunded)
        {
            throw new BusinessException("仅支付成功或部分退款的订单可发起退款");
        }

        var refundable = GetRefundableAmount();
        if (refundable <= 0)
        {
            throw new BusinessException("订单无可退金额");
        }

        if (refundAmount <= 0 || refundAmount > refundable)
        {
            throw new BusinessException("退款金额无效");
        }
    }

    public void BeginRefunding()
    {
        if (Status is not PaymentOrderStatus.Success and not PaymentOrderStatus.PartialRefunded)
        {
            throw new BusinessException("当前订单状态不允许发起退款");
        }

        Status = PaymentOrderStatus.Refunding;
    }

    public void RevertRefundingAfterRefundFailed()
    {
        if (Status != PaymentOrderStatus.Refunding)
        {
            return;
        }

        Status = RefundedAmount > 0
            ? PaymentOrderStatus.PartialRefunded
            : PaymentOrderStatus.Success;
    }

    public void RecordRefundSuccess(long refundAmount)
    {
        if (refundAmount <= 0)
        {
            throw new BusinessException("退款金额无效");
        }

        RefundedAmount += refundAmount;
        Status = RefundedAmount >= Amount
            ? PaymentOrderStatus.Refunded
            : PaymentOrderStatus.PartialRefunded;
    }

    private bool CanTransitionToPaid() =>
        Status is not PaymentOrderStatus.Success
            and not PaymentOrderStatus.Refunding
            and not PaymentOrderStatus.Refunded
            and not PaymentOrderStatus.PartialRefunded;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

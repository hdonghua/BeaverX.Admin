using BeaverX.Admin.Domain.Shared;
using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Payment;

/// <summary>退款单</summary>
public class PaymentRefund : FullAuditedEntity
{
    public string RefundNo { get; private set; } = null!;
    public long PaymentOrderId { get; private set; }
    public string OrderNo { get; private set; } = null!;
    public string ChannelCode { get; private set; } = null!;

    /// <summary>退款金额（分）</summary>
    public long Amount { get; private set; }

    /// <summary>原订单金额（分）</summary>
    public long TotalAmount { get; private set; }
    public PaymentRefundStatus Status { get; private set; } = PaymentRefundStatus.Pending;
    public string? ChannelRefundNo { get; private set; }
    public string? ChannelOrderNo { get; private set; }
    public string? Reason { get; private set; }
    public DateTime? RefundTime { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }

    public PaymentOrder? PaymentOrder { get; private set; }

    private PaymentRefund()
    {
    }

    public static PaymentRefund CreatePending(
        PaymentOrder order,
        string refundNo,
        long amount,
        string? reason = null)
    {
        if (order.Id <= 0)
        {
            throw new BusinessException("支付订单尚未持久化，无法创建退款单");
        }

        if (string.IsNullOrWhiteSpace(refundNo))
        {
            throw new BusinessException("退款单号不能为空");
        }

        order.EnsureCanRefund(amount);

        return new PaymentRefund
        {
            RefundNo = refundNo.Trim(),
            PaymentOrderId = order.Id,
            OrderNo = order.OrderNo,
            ChannelCode = order.ChannelCode,
            Amount = amount,
            TotalAmount = order.Amount,
            Status = PaymentRefundStatus.Pending,
            ChannelOrderNo = order.ChannelOrderNo,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
        };
    }

    public bool CanApplyNotifySuccess => Status != PaymentRefundStatus.Success;

    public void MarkFailed(string? errorCode, string? errorMessage)
    {
        Status = PaymentRefundStatus.Failed;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage ?? "退款失败";
    }

    public void MarkProcessing(string? channelRefundNo, DateTime? refundTime)
    {
        Status = PaymentRefundStatus.Processing;
        ChannelRefundNo = channelRefundNo ?? ChannelRefundNo;
        RefundTime = refundTime;
    }

    public void MarkSuccess(string? channelRefundNo, DateTime? refundTime)
    {
        Status = PaymentRefundStatus.Success;
        ChannelRefundNo = channelRefundNo ?? ChannelRefundNo;
        RefundTime = refundTime ?? DateTime.UtcNow;
        ErrorCode = null;
        ErrorMessage = null;
    }

    public void ApplyProviderResult(
        bool success,
        PaymentRefundStatus providerStatus,
        string? channelRefundNo,
        DateTime? refundTime)
    {
        if (!success)
        {
            MarkFailed(null, "退款失败");
            return;
        }

        ChannelRefundNo = channelRefundNo ?? ChannelRefundNo;
        RefundTime = refundTime;

        if (providerStatus == PaymentRefundStatus.Success)
        {
            MarkSuccess(channelRefundNo, refundTime);
            return;
        }

        MarkProcessing(channelRefundNo, refundTime);
    }
}

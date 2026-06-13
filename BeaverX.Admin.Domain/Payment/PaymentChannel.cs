using BeaverX.Admin.Domain.Shared;
using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Payment;

/// <summary>支付渠道配置（密钥、证书等存于 <see cref="ConfigJson"/>）</summary>
public class PaymentChannel : FullAuditedEntity
{
    /// <summary>渠道编码，见 <see cref="PaymentChannelCodes"/></summary>
    public string ChannelCode { get; private set; } = null!;

    /// <summary>渠道显示名称</summary>
    public string ChannelName { get; private set; } = null!;

    /// <summary>支付提供商类型</summary>
    public PaymentProviderType ProviderType { get; private set; }

    /// <summary>是否启用</summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>JSON 配置：AppId、商户号、API 密钥、证书路径或内容等</summary>
    public string ConfigJson { get; private set; } = "{}";

    /// <summary>可选：覆盖系统默认的支付/退款回调地址</summary>
    public string? NotifyUrl { get; private set; }

    public string? Remark { get; private set; }
    public int Sort { get; private set; }

    private PaymentChannel()
    {
    }

    public static PaymentChannel Create(
        string channelCode,
        string channelName,
        PaymentProviderType providerType,
        bool isEnabled = true,
        string? configJson = null,
        string? notifyUrl = null,
        string? remark = null,
        int sort = 0)
    {
        if (string.IsNullOrWhiteSpace(channelCode))
        {
            throw new BusinessException("渠道编码不能为空");
        }

        if (string.IsNullOrWhiteSpace(channelName))
        {
            throw new BusinessException("渠道名称不能为空");
        }

        return new PaymentChannel
        {
            ChannelCode = channelCode.Trim(),
            ChannelName = channelName.Trim(),
            ProviderType = providerType,
            IsEnabled = isEnabled,
            ConfigJson = string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson.Trim(),
            NotifyUrl = NormalizeOptional(notifyUrl),
            Remark = NormalizeOptional(remark),
            Sort = sort,
        };
    }

    public void EnsureAvailableForPayment()
    {
        if (!IsEnabled)
        {
            throw new BusinessException($"支付渠道不可用: {ChannelCode}");
        }
    }

    public void Rename(string channelName)
    {
        if (string.IsNullOrWhiteSpace(channelName))
        {
            throw new BusinessException("渠道名称不能为空");
        }

        ChannelName = channelName.Trim();
    }

    public void SetEnabled(bool isEnabled) => IsEnabled = isEnabled;

    public void UpdateConfigJson(string configJson)
    {
        ConfigJson = string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson.Trim();
    }

    public void SetNotifyUrl(string? notifyUrl) => NotifyUrl = NormalizeOptional(notifyUrl);

    public void SetRemark(string? remark) => Remark = NormalizeOptional(remark);

    public void SetSort(int sort) => Sort = sort;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

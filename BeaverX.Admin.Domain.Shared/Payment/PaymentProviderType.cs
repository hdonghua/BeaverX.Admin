namespace BeaverX.Admin.Domain.Shared.Payment;

/// <summary>支付提供商类型（与渠道配置表单、Provider 实现对应）</summary>
public enum PaymentProviderType
{
  /// <summary>微信支付（二维码）</summary>
  WeChat = 1,

  /// <summary>支付宝（二维码当面付）</summary>
  Alipay = 2,

  /// <summary>支付宝（App 调起）</summary>
  AlipayApp = 3,
}

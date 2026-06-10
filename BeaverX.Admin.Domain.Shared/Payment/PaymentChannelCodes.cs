namespace BeaverX.Admin.Domain.Shared.Payment;

/// <summary>内置支付渠道编码常量</summary>
public static class PaymentChannelCodes
{
  /// <summary>微信二维码支付（API: POST /v3/pay/transactions/native）</summary>
  public const string WeChatQrcode = "wechat_qrcode";

  /// <summary>支付宝二维码支付（API: alipay.trade.precreate，product_code: QR_CODE_OFFLINE）</summary>
  public const string AlipayQrcode = "alipay_qrcode";

  /// <summary>支付宝 App 支付（API: alipay.trade.app.pay，product_code: QUICK_MSECURITY_PAY）</summary>
  public const string AlipayAppPay = "alipay_app_pay";

  /// <summary>是否为支付宝系渠道（二维码或 App）</summary>
  public static bool IsAlipay(string channelCode) =>
    channelCode == AlipayQrcode || channelCode == AlipayAppPay;

  /// <summary>是否为二维码扫码渠道（微信或支付宝）</summary>
  public static bool IsQrcodeChannel(string channelCode) =>
    channelCode == WeChatQrcode || channelCode == AlipayQrcode;
}

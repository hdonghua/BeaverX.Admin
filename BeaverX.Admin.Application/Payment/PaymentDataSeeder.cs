using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Admin.Domain.Payment;
using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Payment;

public class PaymentDataSeeder : IScopedDependency, IDataSeeder
{
  private readonly IRepository<PaymentChannel> _channelRepository;
  private readonly ILogger<PaymentDataSeeder> _logger;

  public PaymentDataSeeder(
    IRepository<PaymentChannel> channelRepository,
    ILogger<PaymentDataSeeder> logger)
  {
    _channelRepository = channelRepository;
    _logger = logger;
  }

  public async Task SeedAsync(CancellationToken cancellationToken = default)
  {
    await EnsureChannelAsync(
      PaymentChannelCodes.WeChatQrcode,
      () => new PaymentChannel
      {
        ChannelCode = PaymentChannelCodes.WeChatQrcode,
        ChannelName = "微信二维码支付",
        ProviderType = PaymentProviderType.WeChat,
        IsEnabled = false,
        ConfigJson = "{\"appId\":\"\",\"mchId\":\"\",\"apiV3Key\":\"\",\"certSerialNo\":\"\",\"privateKey\":\"\",\"platformCert\":\"\"}",
        Sort = 1,
        Remark = "微信二维码支付（API: POST /v3/pay/transactions/native）",
      },
      cancellationToken);

    await EnsureChannelAsync(
      PaymentChannelCodes.AlipayQrcode,
      () => new PaymentChannel
      {
        ChannelCode = PaymentChannelCodes.AlipayQrcode,
        ChannelName = "支付宝二维码支付",
        ProviderType = PaymentProviderType.Alipay,
        IsEnabled = false,
        ConfigJson = "{\"appId\":\"\",\"privateKey\":\"\",\"alipayPublicKey\":\"\",\"merchantCertPath\":\"\",\"alipayPublicCertPath\":\"\",\"alipayRootCertPath\":\"\",\"signType\":\"RSA2\",\"gateway\":\"https://openapi.alipay.com/gateway.do\"}",
        Sort = 2,
        Remark = "支付宝当面付扫码（product_code: QR_CODE_OFFLINE）",
      },
      cancellationToken);

    await EnsureChannelAsync(
      PaymentChannelCodes.AlipayAppPay,
      () => new PaymentChannel
      {
        ChannelCode = PaymentChannelCodes.AlipayAppPay,
        ChannelName = "支付宝APP支付",
        ProviderType = PaymentProviderType.AlipayApp,
        IsEnabled = false,
        ConfigJson = "{\"appId\":\"\",\"privateKey\":\"\",\"alipayPublicKey\":\"\",\"merchantCertPath\":\"\",\"alipayPublicCertPath\":\"\",\"alipayRootCertPath\":\"\",\"signType\":\"RSA2\",\"gateway\":\"https://openapi.alipay.com/gateway.do\"}",
        Sort = 3,
        Remark = "支付宝 App 支付（product_code: QUICK_MSECURITY_PAY）",
      },
      cancellationToken);
  }

  private async Task EnsureChannelAsync(
    string channelCode,
    Func<PaymentChannel> factory,
    CancellationToken cancellationToken)
  {
    if (await _channelRepository.AnyAsync(x => x.ChannelCode == channelCode, cancellationToken))
    {
      return;
    }

    _logger.LogInformation("Seeding payment channel {ChannelCode}...", channelCode);
    await _channelRepository.InsertAsync(factory(), cancellationToken: cancellationToken);
  }
}

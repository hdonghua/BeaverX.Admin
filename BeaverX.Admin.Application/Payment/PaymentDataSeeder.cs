using BeaverX.Admin.Application.Contracts.Demo;
using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Admin.Domain.Payment;
using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Payment;

public class PaymentDataSeeder : IScopedDependency, IDataSeeder, IOverwriteDataSeeder
{
    private readonly IRepository<PaymentChannel> _channelRepository;
    private readonly IDemoDatabaseHardResetService _demoHardResetService;
    private readonly ILogger<PaymentDataSeeder> _logger;

    public PaymentDataSeeder(
      IRepository<PaymentChannel> channelRepository,
      IDemoDatabaseHardResetService demoHardResetService,
      ILogger<PaymentDataSeeder> logger)
    {
        _channelRepository = channelRepository;
        _demoHardResetService = demoHardResetService;
        _logger = logger;
    }

    public int Order => 40;

    public async Task OverwriteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Overwriting payment channel demo data...");
        await _demoHardResetService.ClearPaymentChannelsAsync(cancellationToken);
        await SeedAsync(cancellationToken);
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureChannelAsync(
          PaymentChannelCodes.WeChatQrcode,
          () => PaymentChannel.Create(
            PaymentChannelCodes.WeChatQrcode,
            "微信二维码支付",
            PaymentProviderType.WeChat,
            isEnabled: false,
            configJson: "{\"appId\":\"\",\"mchId\":\"\",\"apiV3Key\":\"\",\"certSerialNo\":\"\",\"privateKey\":\"\",\"platformCert\":\"\"}",
            remark: "微信二维码支付（API: POST /v3/pay/transactions/native）",
            sort: 1),
          cancellationToken);

        await EnsureChannelAsync(
          PaymentChannelCodes.AlipayQrcode,
          () => PaymentChannel.Create(
            PaymentChannelCodes.AlipayQrcode,
            "支付宝二维码支付",
            PaymentProviderType.Alipay,
            isEnabled: false,
            configJson: "{\"appId\":\"\",\"privateKey\":\"\",\"alipayPublicKey\":\"\",\"merchantCertPath\":\"\",\"alipayPublicCertPath\":\"\",\"alipayRootCertPath\":\"\",\"signType\":\"RSA2\",\"gateway\":\"https://openapi.alipay.com/gateway.do\"}",
            remark: "支付宝当面付扫码（product_code: QR_CODE_OFFLINE）",
            sort: 2),
          cancellationToken);

        await EnsureChannelAsync(
          PaymentChannelCodes.AlipayAppPay,
          () => PaymentChannel.Create(
            PaymentChannelCodes.AlipayAppPay,
            "支付宝APP支付",
            PaymentProviderType.AlipayApp,
            isEnabled: false,
            configJson: "{\"appId\":\"\",\"privateKey\":\"\",\"alipayPublicKey\":\"\",\"merchantCertPath\":\"\",\"alipayPublicCertPath\":\"\",\"alipayRootCertPath\":\"\",\"signType\":\"RSA2\",\"gateway\":\"https://openapi.alipay.com/gateway.do\"}",
            remark: "支付宝 App 支付（product_code: QUICK_MSECURITY_PAY）",
            sort: 3),
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

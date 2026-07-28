using BeaverX.Admin.Domain.Payment;
using BeaverX.Admin.Domain.Shared.Payment;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Payment;

public class PaymentDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<PaymentChannel, Guid> _channelRepository;
    private readonly ILogger<PaymentDataSeeder> _logger;

    public PaymentDataSeeder(
      IRepository<PaymentChannel, Guid> channelRepository,
      ILogger<PaymentDataSeeder> logger)
    {
        _channelRepository = channelRepository;
        _logger = logger;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        var cancellationToken = CancellationToken.None;
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

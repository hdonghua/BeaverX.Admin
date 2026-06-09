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
    if (await _channelRepository.AnyAsync(
        x => x.ChannelCode == PaymentChannelCodes.SandboxNative,
        cancellationToken))
    {
      return;
    }

    _logger.LogInformation("Seeding default payment channels...");

    await _channelRepository.InsertManyAsync([
      new PaymentChannel
      {
        ChannelCode = PaymentChannelCodes.SandboxNative,
        ChannelName = "沙箱扫码支付",
        ProviderType = PaymentProviderType.Sandbox,
        IsEnabled = true,
        ConfigJson = "{}",
        Sort = 0,
        Remark = "本地联调用，配合模拟支付按钮",
      },
      new PaymentChannel
      {
        ChannelCode = PaymentChannelCodes.WeChatNative,
        ChannelName = "微信 Native 扫码",
        ProviderType = PaymentProviderType.WeChat,
        IsEnabled = false,
        ConfigJson = "{\"appId\":\"\",\"mchId\":\"\",\"apiV3Key\":\"\",\"certSerialNo\":\"\",\"privateKey\":\"\",\"platformCert\":\"\"}",
        Sort = 1,
        Remark = "配置 AppId、商户号、APIv3 密钥、证书序列号、商户私钥、平台证书",
      },
      new PaymentChannel
      {
        ChannelCode = PaymentChannelCodes.AlipayNative,
        ChannelName = "支付宝当面付扫码",
        ProviderType = PaymentProviderType.Alipay,
        IsEnabled = false,
        ConfigJson = "{\"appId\":\"\",\"privateKey\":\"\",\"alipayPublicKey\":\"\",\"merchantCertPath\":\"\",\"alipayPublicCertPath\":\"\",\"alipayRootCertPath\":\"\",\"signType\":\"RSA2\",\"gateway\":\"https://openapi.alipay.com/gateway.do\"}",
        Sort = 2,
        Remark = "配置 AppId、应用私钥、支付宝公钥",
      },
    ], cancellationToken: cancellationToken);
  }
}

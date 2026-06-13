using System.Text.Json;
using System.Text.Json.Serialization;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Domain.Shared.Payment;

namespace BeaverX.Admin.Application.Payment;

public static class PaymentChannelConfigValidator
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true,
  };

  public static void Validate(PaymentProviderType providerType, string? configJson)
  {
    if (string.IsNullOrWhiteSpace(configJson))
    {
      throw new BusinessException("渠道配置不能为空");
    }

    try
    {
      switch (providerType)
      {
        case PaymentProviderType.Alipay:
        case PaymentProviderType.AlipayApp:
          ValidateAlipayConfig(configJson);
          break;
        case PaymentProviderType.WeChat:
          ValidateWeChatConfig(configJson);
          break;
        default:
          throw new BusinessException($"不支持的支付提供商类型: {providerType}");
      }
    }
    catch (JsonException)
    {
      throw new BusinessException("渠道配置 JSON 格式无效");
    }
  }

  private static void ValidateAlipayConfig(string configJson)
  {
    var config = JsonSerializer.Deserialize<AlipayChannelConfigPayload>(configJson, JsonOptions)
      ?? new AlipayChannelConfigPayload();

    if (string.IsNullOrWhiteSpace(config.AppId))
    {
      throw new BusinessException("请填写支付宝 AppId");
    }

    if (string.IsNullOrWhiteSpace(config.PrivateKey))
    {
      throw new BusinessException("请填写应用私钥");
    }

    if (!AlipayPaymentConstants.IsSupportedSignType(config.SignType))
    {
      throw new BusinessException(
        $"签名类型无效，仅支持：{string.Join("、", AlipayPaymentConstants.SupportedSignTypes)}");
    }

    if (!AlipayPaymentConstants.IsSupportedGateway(config.Gateway))
    {
      throw new BusinessException(
        $"网关地址无效，仅支持：{string.Join("、", AlipayPaymentConstants.SupportedGateways)}");
    }

    var certMode = HasValue(config.MerchantCertUrl) &&
                   HasValue(config.AlipayPublicCertUrl) &&
                   HasValue(config.AlipayRootCertUrl) &&
                   HasValue(config.MerchantCertFileName) &&
                   HasValue(config.AlipayPublicCertFileName) &&
                   HasValue(config.AlipayRootCertFileName);

    if (certMode)
    {
      return;
    }

    if (HasAnyCertField(config))
    {
      throw new BusinessException("证书模式需上传完整三项证书");
    }

    if (string.IsNullOrWhiteSpace(config.AlipayPublicKey))
    {
      throw new BusinessException("公钥模式需填写支付宝公钥，或上传完整三项证书");
    }
  }

  private static bool HasAnyCertField(AlipayChannelConfigPayload config) =>
    HasValue(config.MerchantCertUrl) ||
    HasValue(config.AlipayPublicCertUrl) ||
    HasValue(config.AlipayRootCertUrl) ||
    HasValue(config.MerchantCertPath) ||
    HasValue(config.AlipayPublicCertPath) ||
    HasValue(config.AlipayRootCertPath);

  private static void ValidateWeChatConfig(string configJson)
  {
    var config = JsonSerializer.Deserialize<WeChatChannelConfigPayload>(configJson, JsonOptions)
      ?? new WeChatChannelConfigPayload();

    if (string.IsNullOrWhiteSpace(config.AppId))
    {
      throw new BusinessException("请填写微信 AppId");
    }

    if (string.IsNullOrWhiteSpace(config.MchId))
    {
      throw new BusinessException("请填写微信商户号");
    }

    if (string.IsNullOrWhiteSpace(config.ApiV3Key))
    {
      throw new BusinessException("请填写 APIv3 密钥");
    }

    if (string.IsNullOrWhiteSpace(config.CertSerialNo))
    {
      throw new BusinessException("请填写证书序列号");
    }

    if (string.IsNullOrWhiteSpace(config.PrivateKey))
    {
      throw new BusinessException("请填写商户私钥");
    }

    if (string.IsNullOrWhiteSpace(config.PlatformCert))
    {
      throw new BusinessException("请填写微信平台证书");
    }
  }

  private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

  private sealed class AlipayChannelConfigPayload
  {
    public string? AppId { get; set; }
    public string? PrivateKey { get; set; }
    public string? AlipayPublicKey { get; set; }
    public string? SignType { get; set; }
    public string? Gateway { get; set; }
    public string? MerchantCertUrl { get; set; }
    public string? MerchantCertPath { get; set; }
    public string? MerchantCertFileName { get; set; }
    public string? AlipayPublicCertUrl { get; set; }
    public string? AlipayPublicCertPath { get; set; }
    public string? AlipayPublicCertFileName { get; set; }
    public string? AlipayRootCertUrl { get; set; }
    public string? AlipayRootCertPath { get; set; }
    public string? AlipayRootCertFileName { get; set; }
  }

  private sealed class WeChatChannelConfigPayload
  {
    public string? AppId { get; set; }
    public string? MchId { get; set; }

    [JsonPropertyName("apiV3Key")]
    public string? ApiV3Key { get; set; }

    public string? CertSerialNo { get; set; }
    public string? PrivateKey { get; set; }
    public string? PlatformCert { get; set; }
  }
}

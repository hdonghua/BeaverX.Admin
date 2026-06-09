using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Core.Dependency;

namespace BeaverX.Admin.Infrastructure.Payment.WeChat;

public class WeChatNativePaymentProvider : IPaymentProvider, IScopedDependency
{
  private const string NativeUrl = "https://api.mch.weixin.qq.com/v3/pay/transactions/native";
  private const string QueryUrlTemplate = "https://api.mch.weixin.qq.com/v3/pay/transactions/out-trade-no/{0}?mchid={1}";
  private const string RefundUrl = "https://api.mch.weixin.qq.com/v3/refund/domestic/refunds";

  private readonly IHttpClientFactory _httpClientFactory;

  public WeChatNativePaymentProvider(IHttpClientFactory httpClientFactory)
  {
    _httpClientFactory = httpClientFactory;
  }

  public string ChannelCode => PaymentChannelCodes.WeChatNative;

  public async Task<NativePayResult> CreateNativePayAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    string notifyUrl,
    CancellationToken cancellationToken = default)
  {
    var config = PaymentConfigHelper.ParseConfig<WeChatChannelConfig>(channel.ConfigJson);
    if (!ValidateConfig(config, out var error))
    {
      return FailNative("CONFIG", error);
    }

    var payload = new
    {
      appid = config.AppId,
      mchid = config.MchId,
      description = order.Subject,
      out_trade_no = order.OrderNo,
      notify_url = notifyUrl,
      amount = new { total = (int)order.Amount, currency = order.Currency },
      attach = order.Attach,
      time_expire = order.ExpireTime?.ToString("yyyy-MM-ddTHH:mm:ss+08:00"),
    };

    var body = JsonSerializer.Serialize(payload);
    var response = await SendAsync(config, NativeUrl, "POST", body, cancellationToken);
    if (!response.Success)
    {
      return FailNative(response.ErrorCode, response.ErrorMessage);
    }

    var codeUrl = response.GetString("code_url");
    if (string.IsNullOrWhiteSpace(codeUrl))
    {
      return FailNative("NO_QR", "微信未返回二维码链接");
    }

    return new NativePayResult
    {
      Success = true,
      QrCodeUrl = codeUrl,
      ChannelOrderNo = order.OrderNo,
    };
  }

  public async Task<QueryPayResult> QueryPayAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    CancellationToken cancellationToken = default)
  {
    var config = PaymentConfigHelper.ParseConfig<WeChatChannelConfig>(channel.ConfigJson);
    if (!ValidateConfig(config, out var error))
    {
      return new QueryPayResult
      {
        Status = PaymentOrderStatus.Paying,
        ErrorMessage = error,
      };
    }

    var url = string.Format(
      CultureInfo.InvariantCulture,
      QueryUrlTemplate,
      Uri.EscapeDataString(order.OrderNo),
      Uri.EscapeDataString(config.MchId));

    var response = await SendAsync(config, url, "GET", null, cancellationToken);
    if (!response.Success)
    {
      return new QueryPayResult
      {
        Status = PaymentOrderStatus.Paying,
        ErrorMessage = response.ErrorMessage,
      };
    }

    var tradeState = response.GetString("trade_state");
    var status = tradeState switch
    {
      "SUCCESS" => PaymentOrderStatus.Success,
      "CLOSED" or "REVOKED" or "PAYERROR" => PaymentOrderStatus.Closed,
      _ => PaymentOrderStatus.Paying,
    };

    DateTime? paidTime = null;
    var successTime = response.GetString("success_time");
    if (!string.IsNullOrWhiteSpace(successTime) &&
        DateTime.TryParse(successTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
    {
      paidTime = parsed;
    }

    return new QueryPayResult
    {
      Status = status,
      ChannelOrderNo = response.GetString("transaction_id"),
      PaidTime = paidTime,
    };
  }

  public async Task<RefundProviderResult> RefundAsync(
    PaymentProviderChannelContext channel,
    PaymentProviderOrderContext order,
    PaymentProviderRefundContext refund,
    string notifyUrl,
    CancellationToken cancellationToken = default)
  {
    var config = PaymentConfigHelper.ParseConfig<WeChatChannelConfig>(channel.ConfigJson);
    if (!ValidateConfig(config, out var error))
    {
      return new RefundProviderResult
      {
        Success = false,
        Status = PaymentRefundStatus.Failed,
        ErrorMessage = error,
      };
    }

    var payload = new
    {
      out_trade_no = order.OrderNo,
      out_refund_no = refund.RefundNo,
      reason = refund.Reason,
      notify_url = notifyUrl,
      amount = new
      {
        refund = (int)refund.Amount,
        total = (int)refund.TotalAmount,
        currency = order.Currency,
      },
    };

    var body = JsonSerializer.Serialize(payload);
    var response = await SendAsync(config, RefundUrl, "POST", body, cancellationToken);
    if (!response.Success)
    {
      return new RefundProviderResult
      {
        Success = false,
        Status = PaymentRefundStatus.Failed,
        ErrorCode = response.ErrorCode,
        ErrorMessage = response.ErrorMessage,
      };
    }

    var refundStatus = response.GetString("status");
    var status = refundStatus == "SUCCESS"
      ? PaymentRefundStatus.Success
      : PaymentRefundStatus.Processing;

    return new RefundProviderResult
    {
      Success = true,
      Status = status,
      ChannelRefundNo = response.GetString("refund_id"),
      RefundTime = status == PaymentRefundStatus.Success ? DateTime.UtcNow : null,
    };
  }

  public Task<NotifyHandleResult> HandlePayNotifyAsync(
    PaymentProviderChannelContext channel,
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default)
  {
    var config = PaymentConfigHelper.ParseConfig<WeChatChannelConfig>(channel.ConfigJson);
    if (!VerifyNotifySignature(config, context, out var error))
    {
      return Task.FromResult(new NotifyHandleResult
      {
        Success = false,
        ResponseBody = JsonSerializer.Serialize(new { code = "FAIL", message = error }),
        ProcessMessage = error,
      });
    }

    try
    {
      using var doc = JsonDocument.Parse(context.RawBody);
      var resource = doc.RootElement.GetProperty("resource");
      var plain = DecryptResource(resource, config.ApiV3Key);
      using var plainDoc = JsonDocument.Parse(plain);
      var tradeState = plainDoc.RootElement.GetProperty("trade_state").GetString();
      if (tradeState != "SUCCESS")
      {
        return Task.FromResult(new NotifyHandleResult
        {
          Success = true,
          ResponseBody = JsonSerializer.Serialize(new { code = "SUCCESS", message = "OK" }),
          ProcessMessage = $"忽略状态: {tradeState}",
        });
      }

      var orderNo = plainDoc.RootElement.GetProperty("out_trade_no").GetString();
      return Task.FromResult(new NotifyHandleResult
      {
        Success = !string.IsNullOrWhiteSpace(orderNo),
        OrderNo = orderNo,
        ResponseBody = JsonSerializer.Serialize(new { code = "SUCCESS", message = "OK" }),
        ProcessMessage = "wechat pay notify",
      });
    }
    catch (Exception ex)
    {
      return Task.FromResult(new NotifyHandleResult
      {
        Success = false,
        ResponseBody = JsonSerializer.Serialize(new { code = "FAIL", message = ex.Message }),
        ProcessMessage = ex.Message,
      });
    }
  }

  public Task<NotifyHandleResult> HandleRefundNotifyAsync(
    PaymentProviderChannelContext channel,
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default)
  {
    var config = PaymentConfigHelper.ParseConfig<WeChatChannelConfig>(channel.ConfigJson);
    if (!VerifyNotifySignature(config, context, out var error))
    {
      return Task.FromResult(new NotifyHandleResult
      {
        Success = false,
        ResponseBody = JsonSerializer.Serialize(new { code = "FAIL", message = error }),
        ProcessMessage = error,
      });
    }

    try
    {
      using var doc = JsonDocument.Parse(context.RawBody);
      var resource = doc.RootElement.GetProperty("resource");
      var plain = DecryptResource(resource, config.ApiV3Key);
      using var plainDoc = JsonDocument.Parse(plain);
      var refundStatus = plainDoc.RootElement.GetProperty("refund_status").GetString();
      if (refundStatus != "SUCCESS")
      {
        return Task.FromResult(new NotifyHandleResult
        {
          Success = true,
          ResponseBody = JsonSerializer.Serialize(new { code = "SUCCESS", message = "OK" }),
          ProcessMessage = $"忽略退款状态: {refundStatus}",
        });
      }

      var refundNo = plainDoc.RootElement.GetProperty("out_refund_no").GetString();
      return Task.FromResult(new NotifyHandleResult
      {
        Success = !string.IsNullOrWhiteSpace(refundNo),
        RefundNo = refundNo,
        ResponseBody = JsonSerializer.Serialize(new { code = "SUCCESS", message = "OK" }),
        ProcessMessage = "wechat refund notify",
      });
    }
    catch (Exception ex)
    {
      return Task.FromResult(new NotifyHandleResult
      {
        Success = false,
        ResponseBody = JsonSerializer.Serialize(new { code = "FAIL", message = ex.Message }),
        ProcessMessage = ex.Message,
      });
    }
  }

  private async Task<WeChatApiResponse> SendAsync(
    WeChatChannelConfig config,
    string url,
    string method,
    string? body,
    CancellationToken cancellationToken)
  {
    var client = _httpClientFactory.CreateClient(nameof(WeChatNativePaymentProvider));
    using var request = new HttpRequestMessage(new HttpMethod(method), url);
    if (!string.IsNullOrEmpty(body))
    {
      request.Content = new StringContent(body, Encoding.UTF8, "application/json");
    }

    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    var nonce = Guid.NewGuid().ToString("N");
    var message = $"{method}\n{GetUrlPath(url)}\n{timestamp}\n{nonce}\n{body ?? string.Empty}\n";
    var signature = Sign(message, config.PrivateKey);
    request.Headers.Authorization = new AuthenticationHeaderValue(
      "WECHATPAY2-SHA256-RSA2048",
      $"mchid=\"{config.MchId}\",nonce_str=\"{nonce}\",signature=\"{signature}\",timestamp=\"{timestamp}\",serial_no=\"{config.CertSerialNo}\"");

    using var response = await client.SendAsync(request, cancellationToken);
    var json = await response.Content.ReadAsStringAsync(cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
      return WeChatApiResponse.Fail(response.StatusCode.ToString(), json);
    }

    try
    {
      using var doc = JsonDocument.Parse(json);
      return WeChatApiResponse.Ok(doc.RootElement);
    }
    catch
    {
      return WeChatApiResponse.Fail("PARSE", json);
    }
  }

  private static bool ValidateConfig(WeChatChannelConfig config, out string error)
  {
    if (string.IsNullOrWhiteSpace(config.AppId) ||
        string.IsNullOrWhiteSpace(config.MchId) ||
        string.IsNullOrWhiteSpace(config.ApiV3Key) ||
        string.IsNullOrWhiteSpace(config.CertSerialNo) ||
        string.IsNullOrWhiteSpace(config.PrivateKey))
    {
      error = "微信 AppId、商户号、APIv3 密钥、证书序列号或私钥未配置";
      return false;
    }

    error = string.Empty;
    return true;
  }

  private static bool VerifyNotifySignature(
    WeChatChannelConfig config,
    PaymentNotifyContext context,
    out string error)
  {
    var timestamp = context.Headers.GetValueOrDefault("Wechatpay-Timestamp");
    var nonce = context.Headers.GetValueOrDefault("Wechatpay-Nonce");
    var signature = context.Headers.GetValueOrDefault("Wechatpay-Signature");
    var serial = context.Headers.GetValueOrDefault("Wechatpay-Serial");

    if (string.IsNullOrWhiteSpace(timestamp) ||
        string.IsNullOrWhiteSpace(nonce) ||
        string.IsNullOrWhiteSpace(signature))
    {
      error = "微信回调头信息不完整";
      return false;
    }

    if (string.IsNullOrWhiteSpace(config.PlatformCert))
    {
      error = "未配置微信平台证书，无法验签";
      return false;
    }

    var message = $"{timestamp}\n{nonce}\n{context.RawBody}\n";
    using var rsa = RSA.Create();
    rsa.ImportFromPem(NormalizePem(config.PlatformCert, "CERTIFICATE"));
    var valid = rsa.VerifyData(
      Encoding.UTF8.GetBytes(message),
      Convert.FromBase64String(signature),
      HashAlgorithmName.SHA256,
      RSASignaturePadding.Pkcs1);

    if (!valid)
    {
      error = $"微信回调验签失败 serial={serial}";
      return false;
    }

    error = string.Empty;
    return true;
  }

  private static string DecryptResource(JsonElement resource, string apiV3Key)
  {
    var associatedData = resource.GetProperty("associated_data").GetString() ?? string.Empty;
    var nonce = resource.GetProperty("nonce").GetString() ?? string.Empty;
    var ciphertext = resource.GetProperty("ciphertext").GetString() ?? string.Empty;
    var cipherBytes = Convert.FromBase64String(ciphertext);
    var tag = cipherBytes[^16..];
    var data = cipherBytes[..^16];
    var plain = new byte[data.Length];
    using var aesGcm = new AesGcm(Encoding.UTF8.GetBytes(apiV3Key), 16);
    aesGcm.Decrypt(
      Encoding.UTF8.GetBytes(nonce),
      data,
      tag,
      plain,
      Encoding.UTF8.GetBytes(associatedData));
    return Encoding.UTF8.GetString(plain);
  }

  private static string Sign(string message, string privateKey)
  {
    using var rsa = RSA.Create();
    rsa.ImportFromPem(NormalizePem(privateKey, "PRIVATE KEY"));
    var signature = rsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    return Convert.ToBase64String(signature);
  }

  private static string GetUrlPath(string url)
  {
    var uri = new Uri(url);
    return uri.PathAndQuery;
  }

  private static string NormalizePem(string key, string label)
  {
    if (key.Contains("BEGIN", StringComparison.Ordinal))
    {
      return key.Replace("\\n", "\n");
    }

    return $"-----BEGIN {label}-----\n{key}\n-----END {label}-----";
  }

  private static NativePayResult FailNative(string? code, string? message) => new()
  {
    Success = false,
    ErrorCode = code,
    ErrorMessage = message,
  };

  private sealed class WeChatApiResponse
  {
    private readonly JsonElement? _node;

    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    private WeChatApiResponse(JsonElement? node)
    {
      _node = node;
    }

    public static WeChatApiResponse Ok(JsonElement node) => new(node)
    {
      Success = true,
    };

    public static WeChatApiResponse Fail(string code, string message) => new(null)
    {
      Success = false,
      ErrorCode = code,
      ErrorMessage = message,
    };

    public string? GetString(string name)
    {
      if (_node == null || !_node.Value.TryGetProperty(name, out var property))
      {
        return null;
      }

      return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }
  }
}

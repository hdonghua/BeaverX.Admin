using System.Text.Json;
using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Application.Contracts.Storage;
using BeaverX.Admin.Infrastructure.Payment.Alipay;
using BeaverX.Core.Dependency;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Infrastructure.Payment;

public class PaymentChannelCertMaterializer : IPaymentChannelCertMaterializer, IScopedDependency
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IBlobStorage _blobStorage;
    private readonly PaymentOptions _options;

    public PaymentChannelCertMaterializer(
      IBlobStorage blobStorage,
      IOptions<PaymentOptions> options)
    {
        _blobStorage = blobStorage;
        _options = options.Value;
    }

    public async Task<string> ResolveAlipayConfigJsonAsync(
      long channelId,
      string configJson,
      CancellationToken cancellationToken = default)
    {
        var config = PaymentConfigHelper.ParseConfig<AlipayChannelConfig>(configJson);
        var changed = false;

        changed |= await EnsureCertFileAsync(
          config.MerchantCertUrl,
          config.MerchantCertPath,
          value => config.MerchantCertPath = value,
          cancellationToken);

        changed |= await EnsureCertFileAsync(
          config.AlipayPublicCertUrl,
          config.AlipayPublicCertPath,
          value => config.AlipayPublicCertPath = value,
          cancellationToken);

        changed |= await EnsureCertFileAsync(
          config.AlipayRootCertUrl,
          config.AlipayRootCertPath,
          value => config.AlipayRootCertPath = value,
          cancellationToken);

        if (!changed)
        {
            return configJson;
        }

        return JsonSerializer.Serialize(config, JsonOptions);
    }

    private async Task<bool> EnsureCertFileAsync(
      string? certUrl,
      string? relativePath,
      Action<string> setAbsolutePath,
      CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(certUrl) || string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var normalizedRelative = relativePath.Trim().Replace('\\', '/');
        var localAbsolute = Path.GetFullPath(
          Path.Combine(_options.CertCacheRootPath, normalizedRelative));

        if (!File.Exists(localAbsolute))
        {
            var objectKey = ExtractObjectKeyFromProxyUrl(certUrl);
            var blob = await _blobStorage.GetAsync(objectKey, cancellationToken: cancellationToken);
            await using var stream = blob.Content;
            await using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);

            var directory = Path.GetDirectoryName(localAbsolute)!;
            Directory.CreateDirectory(directory);
            await File.WriteAllBytesAsync(localAbsolute, memory.ToArray(), cancellationToken);
        }

        setAbsolutePath(localAbsolute);
        return true;
    }

    internal static string ExtractObjectKeyFromProxyUrl(string certUrl)
    {
        var value = certUrl.Trim();
        const string marker = "/api/File/proxy/";
        var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            value = value[(index + marker.Length)..];
        }

        value = value.TrimStart('/');
        return string.Join('/', value.Split('/', StringSplitOptions.RemoveEmptyEntries)
          .Select(Uri.UnescapeDataString));
    }
}

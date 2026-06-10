using System.Text.Json;
using System.Text.Json.Nodes;
using BeaverX.Admin.Domain.Shared.Payment;

namespace BeaverX.Admin.Application.Payment;

/// <summary>保存渠道时写入证书相对路径（原文件名 + 渠道 Id）</summary>
public static class AlipayChannelConfigNormalizer
{
  public static string Normalize(long channelId, string configJson)
  {
    if (string.IsNullOrWhiteSpace(configJson))
    {
      return "{}";
    }

    JsonNode? root;
    try
    {
      root = JsonNode.Parse(configJson);
    }
    catch (JsonException)
    {
      return configJson;
    }

    if (root is not JsonObject obj)
    {
      return configJson;
    }

    foreach (var (urlKey, pathKey, fileNameKey) in AlipayCertConfigKeys.CertFields)
    {
      var url = obj[urlKey]?.GetValue<string>()?.Trim();
      var fileName = obj[fileNameKey]?.GetValue<string>()?.Trim();
      if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(fileName))
      {
        obj.Remove(pathKey);
        continue;
      }

      obj[pathKey] = BuildRelativeCertPath(fileName, channelId);
    }

    return obj.ToJsonString();
  }

  internal static string BuildRelativeCertPath(string originalFileName, long channelId)
  {
    var safeName = Path.GetFileName(originalFileName.Trim());
    var extension = Path.GetExtension(safeName);
    var baseName = Path.GetFileNameWithoutExtension(safeName);
    if (string.IsNullOrWhiteSpace(baseName))
    {
      baseName = "cert";
    }

    baseName = new string(baseName.Where(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-').ToArray());
    if (string.IsNullOrWhiteSpace(baseName))
    {
      baseName = "cert";
    }

    extension = string.IsNullOrWhiteSpace(extension) ? ".crt" : extension.ToLowerInvariant();
    return $"cert/{baseName}_{channelId}{extension}";
  }
}

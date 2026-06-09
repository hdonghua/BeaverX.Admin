using System.Text.Json;

namespace BeaverX.Admin.Infrastructure.Payment;

internal static class PaymentConfigHelper
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true,
  };

  public static T ParseConfig<T>(string configJson) where T : new()
  {
    if (string.IsNullOrWhiteSpace(configJson))
    {
      return new T();
    }

    try
    {
      return JsonSerializer.Deserialize<T>(configJson, JsonOptions) ?? new T();
    }
    catch
    {
      return new T();
    }
  }
}

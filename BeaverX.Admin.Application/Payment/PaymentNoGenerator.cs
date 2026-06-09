namespace BeaverX.Admin.Application.Payment;

internal static class PaymentNoGenerator
{
  public static string NewOrderNo() =>
    $"PO{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100000, 999999)}";

  public static string NewRefundNo() =>
    $"RF{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100000, 999999)}";
}

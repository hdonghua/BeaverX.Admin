namespace BeaverX.Admin.Domain.Shared.Payment;

public enum PaymentOrderStatus
{
  Pending = 0,
  Paying = 1,
  Success = 2,
  Failed = 3,
  Closed = 4,
  Refunding = 5,
  Refunded = 6,
  PartialRefunded = 7,
}

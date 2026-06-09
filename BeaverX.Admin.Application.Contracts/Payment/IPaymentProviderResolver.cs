namespace BeaverX.Admin.Application.Contracts.Payment;

public interface IPaymentProviderResolver
{
  IPaymentProvider Resolve(string channelCode);
}

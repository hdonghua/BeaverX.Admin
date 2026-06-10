namespace BeaverX.Admin.Application.Contracts.Payment;

public interface IPaymentNotifyAppService
{
  Task<(string Body, int StatusCode)> HandleWeChatPayNotifyAsync(
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default);

  Task<(string Body, int StatusCode)> HandleAlipayNotifyAsync(
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default);

  Task<(string Body, int StatusCode)> HandleWeChatRefundNotifyAsync(
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default);

  Task<(string Body, int StatusCode)> HandleAlipayRefundNotifyAsync(
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default);
}

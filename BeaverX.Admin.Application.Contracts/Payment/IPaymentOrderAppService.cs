using BeaverX.Admin.Application.Contracts.Payment.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Payment;

public interface IPaymentOrderAppService
{
  Task<PagedResultDto<PaymentOrderDto>> GetListAsync(
    PaymentOrderQueryDto input,
    CancellationToken cancellationToken = default);

  Task<PaymentOrderDto> GetAsync(long id, CancellationToken cancellationToken = default);

  Task<PaymentOrderDto> GetByOrderNoAsync(string orderNo, CancellationToken cancellationToken = default);

  Task<CreatePaymentOrderResultDto> CreateNativeOrderAsync(
    CreatePaymentOrderDto input,
    string? clientIp,
    long? userId,
    CancellationToken cancellationToken = default);

  Task<PaymentOrderDto> SyncOrderAsync(long id, CancellationToken cancellationToken = default);

  Task<PaymentOrderDto> CloseOrderAsync(long id, CancellationToken cancellationToken = default);

  Task<PaymentRefundDto> RefundAsync(
    CreatePaymentRefundDto input,
    CancellationToken cancellationToken = default);

  Task<PaymentOrderDto> SandboxPayAsync(string orderNo, CancellationToken cancellationToken = default);
}

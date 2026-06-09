using BeaverX.Admin.Application.Contracts.Payment.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Payment;

public interface IPaymentRefundAppService
{
  Task<PagedResultDto<PaymentRefundDto>> GetListAsync(
    PaymentRefundQueryDto input,
    CancellationToken cancellationToken = default);

  Task<PaymentRefundDto> GetAsync(long id, CancellationToken cancellationToken = default);
}

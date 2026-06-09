using BeaverX.Admin.Application.Contracts.Payment.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Payment;

public interface IPaymentChannelAppService
{
  Task<PagedResultDto<PaymentChannelDto>> GetListAsync(
    PaymentChannelQueryDto input,
    CancellationToken cancellationToken = default);

  Task<List<PaymentChannelDto>> GetEnabledListAsync(CancellationToken cancellationToken = default);

  Task<PaymentChannelDto> GetAsync(long id, CancellationToken cancellationToken = default);

  Task<PaymentChannelDto> CreateAsync(
    CreatePaymentChannelDto input,
    CancellationToken cancellationToken = default);

  Task<PaymentChannelDto> UpdateAsync(
    long id,
    UpdatePaymentChannelDto input,
    CancellationToken cancellationToken = default);

  Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}

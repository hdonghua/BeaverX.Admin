using BeaverX.Admin.Application.Contracts.Payment.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Payment;

/// <summary>支付订单应用服务</summary>
public interface IPaymentOrderAppService
{
    Task<PagedResultDto<PaymentOrderDto>> GetListAsync(
      PaymentOrderQueryDto input,
      CancellationToken cancellationToken = default);

    Task<PaymentOrderDto> GetAsync(long id, CancellationToken cancellationToken = default);

    Task<PaymentOrderDto> GetByOrderNoAsync(string orderNo, CancellationToken cancellationToken = default);

    /// <summary>创建支付订单（二维码或 App，由渠道类型决定）</summary>
    Task<CreatePaymentOrderResultDto> CreatePayOrderAsync(
      CreatePaymentOrderDto input,
      string? clientIp,
      long? userId,
      CancellationToken cancellationToken = default);

    Task<PaymentOrderDto> SyncOrderAsync(long id, CancellationToken cancellationToken = default);

    Task<PaymentOrderDto> CloseOrderAsync(long id, CancellationToken cancellationToken = default);

    Task<PaymentRefundDto> RefundAsync(
      CreatePaymentRefundDto input,
      CancellationToken cancellationToken = default);
}

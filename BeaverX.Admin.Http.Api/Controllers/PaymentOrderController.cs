using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Application.Contracts.Payment.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using BeaverX.WebMvc.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class PaymentOrderController : BeaverXController
{
  private readonly IPaymentOrderAppService _orderAppService;

  public PaymentOrderController(IPaymentOrderAppService orderAppService)
  {
    _orderAppService = orderAppService;
  }

  [RequirePermission(RbacPermissionCodes.Payment.Order.List)]
  [HttpGet("list")]
  public Task<PagedResultDto<PaymentOrderDto>> GetListAsync(
    [FromQuery] PaymentOrderQueryDto input,
    CancellationToken cancellationToken)
    => _orderAppService.GetListAsync(input, cancellationToken);

  [RequirePermission(RbacPermissionCodes.Payment.Order.List)]
  [HttpGet("{id:long}")]
  public Task<PaymentOrderDto> GetAsync(long id, CancellationToken cancellationToken)
    => _orderAppService.GetAsync(id, cancellationToken);

  [RequirePermission(RbacPermissionCodes.Payment.Order.List)]
  [HttpGet("order-no/{orderNo}")]
  public Task<PaymentOrderDto> GetByOrderNoAsync(string orderNo, CancellationToken cancellationToken)
    => _orderAppService.GetByOrderNoAsync(orderNo, cancellationToken);

  [RequirePermission(RbacPermissionCodes.Payment.Order.Create)]
  [HttpPost("pay")]
  public Task<CreatePaymentOrderResultDto> CreatePayOrderAsync(
    [FromBody] CreatePaymentOrderDto input,
    CancellationToken cancellationToken)
  {
    var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
    var userId = CurrentUser.Id;
    return _orderAppService.CreatePayOrderAsync(input, clientIp, userId, cancellationToken);
  }

  [RequirePermission(RbacPermissionCodes.Payment.Order.Query)]
  [HttpPost("{id:long}/sync")]
  public Task<PaymentOrderDto> SyncOrderAsync(long id, CancellationToken cancellationToken)
    => _orderAppService.SyncOrderAsync(id, cancellationToken);

  [RequirePermission(RbacPermissionCodes.Payment.Order.Close)]
  [HttpPost("{id:long}/close")]
  public Task<PaymentOrderDto> CloseOrderAsync(long id, CancellationToken cancellationToken)
    => _orderAppService.CloseOrderAsync(id, cancellationToken);

  [RequirePermission(RbacPermissionCodes.Payment.Order.Refund)]
  [HttpPost("refund")]
  public Task<PaymentRefundDto> RefundAsync(
    [FromBody] CreatePaymentRefundDto input,
    CancellationToken cancellationToken)
    => _orderAppService.RefundAsync(input, cancellationToken);
}

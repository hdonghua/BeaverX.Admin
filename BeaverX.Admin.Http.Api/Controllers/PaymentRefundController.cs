using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Application.Contracts.Payment.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class PaymentRefundController : AdminControllerBase
{
  private readonly IPaymentRefundAppService _refundAppService;

  public PaymentRefundController(IPaymentRefundAppService refundAppService)
  {
    _refundAppService = refundAppService;
  }

  [RequirePermission(RbacPermissionCodes.Payment.Refund.List)]
  [HttpGet("list")]
  public Task<PagedResultDto<PaymentRefundDto>> GetListAsync(
    [FromQuery] PaymentRefundQueryDto input,
    CancellationToken cancellationToken)
    => _refundAppService.GetListAsync(input, cancellationToken);

  [RequirePermission(RbacPermissionCodes.Payment.Refund.List)]
  [HttpGet("{id:guid}")]
  public Task<PaymentRefundDto> GetAsync(Guid id, CancellationToken cancellationToken)
    => _refundAppService.GetAsync(id, cancellationToken: cancellationToken);
}

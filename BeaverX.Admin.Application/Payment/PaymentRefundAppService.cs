using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Application.Contracts.Payment.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Rbac;
using BeaverX.Admin.Domain.Payment;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Payment;

public class PaymentRefundAppService : IPaymentRefundAppService, IScopedDependency
{
    private readonly IRepository<PaymentRefund> _refundRepository;

    public PaymentRefundAppService(IRepository<PaymentRefund> refundRepository)
    {
        _refundRepository = refundRepository;
    }

    public async Task<PagedResultDto<PaymentRefundDto>> GetListAsync(
      PaymentRefundQueryDto input,
      CancellationToken cancellationToken = default)
    {
        var query = _refundRepository.GetQueryable();

        if (!string.IsNullOrWhiteSpace(input.OrderNo))
        {
            var orderNo = input.OrderNo.Trim();
            query = query.Where(x => x.OrderNo.Contains(orderNo));
        }

        if (!string.IsNullOrWhiteSpace(input.RefundNo))
        {
            var refundNo = input.RefundNo.Trim();
            query = query.Where(x => x.RefundNo.Contains(refundNo));
        }

        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var (skip, take) = RbacQueryHelper.GetPaging(input.Page, input.PageSize);
        var items = await query
          .OrderByDescending(x => x.CreationTime)
          .Skip(skip)
          .Take(take)
          .ToListAsync(cancellationToken);

        return new PagedResultDto<PaymentRefundDto>
        {
            Total = total,
            Items = items.Select(PaymentMapper.ToRefundDto).ToList(),
        };
    }

    public async Task<PaymentRefundDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _refundRepository.FindAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            throw new BusinessException($"退款单不存在: {id}");
        }

        return PaymentMapper.ToRefundDto(entity);
    }
}

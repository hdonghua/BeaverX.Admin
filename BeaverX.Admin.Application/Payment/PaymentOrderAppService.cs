using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Application.Contracts.Payment.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Rbac;
using BeaverX.Admin.Domain.Payment;
using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Application.Payment;

public class PaymentOrderAppService : IPaymentOrderAppService, IScopedDependency
{
  private readonly IRepository<PaymentOrder> _orderRepository;
  private readonly IRepository<PaymentChannel> _channelRepository;
  private readonly IRepository<PaymentRefund> _refundRepository;
  private readonly IPaymentProviderResolver _providerResolver;
  private readonly PaymentNotifyUrlBuilder _notifyUrlBuilder;
  private readonly PaymentOptions _paymentOptions;

  public PaymentOrderAppService(
    IRepository<PaymentOrder> orderRepository,
    IRepository<PaymentChannel> channelRepository,
    IRepository<PaymentRefund> refundRepository,
    IPaymentProviderResolver providerResolver,
    PaymentNotifyUrlBuilder notifyUrlBuilder,
    IOptions<PaymentOptions> paymentOptions)
  {
    _orderRepository = orderRepository;
    _channelRepository = channelRepository;
    _refundRepository = refundRepository;
    _providerResolver = providerResolver;
    _notifyUrlBuilder = notifyUrlBuilder;
    _paymentOptions = paymentOptions.Value;
  }

  public async Task<PagedResultDto<PaymentOrderDto>> GetListAsync(
    PaymentOrderQueryDto input,
    CancellationToken cancellationToken = default)
  {
    var query = _orderRepository.GetQueryable();

    if (!string.IsNullOrWhiteSpace(input.OrderNo))
    {
      var orderNo = input.OrderNo.Trim();
      query = query.Where(x => x.OrderNo.Contains(orderNo));
    }

    if (!string.IsNullOrWhiteSpace(input.ChannelCode))
    {
      var channelCode = input.ChannelCode.Trim();
      query = query.Where(x => x.ChannelCode == channelCode);
    }

    if (input.Status.HasValue)
    {
      query = query.Where(x => x.Status == input.Status.Value);
    }

    if (input.StartTime.HasValue)
    {
      query = query.Where(x => x.CreationTime >= input.StartTime.Value);
    }

    if (input.EndTime.HasValue)
    {
      query = query.Where(x => x.CreationTime <= input.EndTime.Value);
    }

    var total = await query.LongCountAsync(cancellationToken);
    var (skip, take) = RbacQueryHelper.GetPaging(input.Page, input.PageSize);
    var items = await query
      .OrderByDescending(x => x.CreationTime)
      .Skip(skip)
      .Take(take)
      .ToListAsync(cancellationToken);

    return new PagedResultDto<PaymentOrderDto>
    {
      Total = total,
      Items = items.Select(PaymentMapper.ToOrderDto).ToList(),
    };
  }

  public async Task<PaymentOrderDto> GetAsync(long id, CancellationToken cancellationToken = default)
  {
    var entity = await FindOrderAsync(id, cancellationToken);
    return PaymentMapper.ToOrderDto(entity);
  }

  public async Task<PaymentOrderDto> GetByOrderNoAsync(
    string orderNo,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(orderNo))
    {
      throw new RbacException("订单号不能为空");
    }

    var entity = await _orderRepository.GetQueryable()
      .FirstOrDefaultAsync(x => x.OrderNo == orderNo.Trim(), cancellationToken);

    if (entity == null)
    {
      throw new RbacException($"支付订单不存在: {orderNo}");
    }

    return PaymentMapper.ToOrderDto(entity);
  }

  public async Task<CreatePaymentOrderResultDto> CreatePayOrderAsync(
    CreatePaymentOrderDto input,
    string? clientIp,
    long? userId,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(input.ChannelCode) ||
        string.IsNullOrWhiteSpace(input.Subject))
    {
      throw new RbacException("支付渠道和订单标题不能为空");
    }

    if (input.Amount <= 0)
    {
      throw new RbacException("支付金额必须大于 0");
    }

    var channelCode = input.ChannelCode.Trim();
    var channel = await _channelRepository.GetQueryable()
      .FirstOrDefaultAsync(x => x.ChannelCode == channelCode && x.IsEnabled, cancellationToken);

    if (channel == null)
    {
      throw new RbacException($"支付渠道不可用: {channelCode}");
    }

    var expireMinutes = input.ExpireMinutes ?? _paymentOptions.DefaultExpireMinutes;
    if (expireMinutes <= 0)
    {
      expireMinutes = _paymentOptions.DefaultExpireMinutes;
    }

    var order = new PaymentOrder
    {
      OrderNo = PaymentNoGenerator.NewOrderNo(),
      ChannelCode = channelCode,
      Subject = input.Subject.Trim(),
      Description = NormalizeOptional(input.Description),
      Amount = input.Amount,
      Currency = "CNY",
      Status = PaymentOrderStatus.Pending,
      ClientIp = NormalizeOptional(clientIp),
      Attach = NormalizeOptional(input.Attach),
      BusinessType = NormalizeOptional(input.BusinessType),
      BusinessId = NormalizeOptional(input.BusinessId),
      UserId = userId,
      ExpireTime = DateTime.UtcNow.AddMinutes(expireMinutes),
    };

    await _orderRepository.InsertAsync(order, cancellationToken: cancellationToken);

    var provider = _providerResolver.Resolve(channelCode);
    var notifyUrl = _notifyUrlBuilder.BuildPayNotifyUrl(channelCode, channel.NotifyUrl);
    var providerChannel = PaymentMapper.ToProviderChannel(channel);
    var providerOrder = PaymentMapper.ToProviderOrder(order);

    string? qrCodeUrl = null;
    string? appPayOrderString = null;
    string? channelOrderNo = null;

    if (channel.ProviderType == PaymentProviderType.AlipayApp)
    {
      var appResult = await provider.CreateAppPayAsync(
        providerChannel,
        providerOrder,
        notifyUrl,
        cancellationToken);

      if (!appResult.Success || string.IsNullOrWhiteSpace(appResult.AppPayOrderString))
      {
        order.Status = PaymentOrderStatus.Failed;
        order.ErrorCode = appResult.ErrorCode;
        order.ErrorMessage = appResult.ErrorMessage ?? "未获取 App 支付参数";
        await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);
        throw new RbacException(order.ErrorMessage);
      }

      appPayOrderString = appResult.AppPayOrderString;
      channelOrderNo = appResult.ChannelOrderNo;
    }
    else
    {
      var qrcodeResult = await provider.CreateQrcodePayAsync(
        providerChannel,
        providerOrder,
        notifyUrl,
        cancellationToken);

      if (!qrcodeResult.Success || string.IsNullOrWhiteSpace(qrcodeResult.QrCodeUrl))
      {
        order.Status = PaymentOrderStatus.Failed;
        order.ErrorCode = qrcodeResult.ErrorCode;
        order.ErrorMessage = qrcodeResult.ErrorMessage ?? "未获取支付二维码";
        await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);
        throw new RbacException(order.ErrorMessage);
      }

      qrCodeUrl = qrcodeResult.QrCodeUrl;
      channelOrderNo = qrcodeResult.ChannelOrderNo;
    }

    order.Status = PaymentOrderStatus.Paying;
    order.QrCodeUrl = qrCodeUrl;
    order.AppPayOrderString = appPayOrderString;
    order.ChannelOrderNo = channelOrderNo;
    order.ErrorCode = null;
    order.ErrorMessage = null;
    await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);

    return new CreatePaymentOrderResultDto
    {
      Order = PaymentMapper.ToOrderDto(order),
      QrCodeUrl = qrCodeUrl,
      AppPayOrderString = appPayOrderString,
    };
  }

  public async Task<PaymentOrderDto> SyncOrderAsync(
    long id,
    CancellationToken cancellationToken = default)
  {
    var order = await FindOrderAsync(id, cancellationToken);
    if (order.Status is PaymentOrderStatus.Success or PaymentOrderStatus.Refunded or PaymentOrderStatus.PartialRefunded)
    {
      return PaymentMapper.ToOrderDto(order);
    }

    var channel = await FindChannelByCodeAsync(order.ChannelCode, cancellationToken);
    var provider = _providerResolver.Resolve(order.ChannelCode);
    var queryResult = await provider.QueryPayAsync(
      PaymentMapper.ToProviderChannel(channel),
      PaymentMapper.ToProviderOrder(order),
      cancellationToken);

    await ApplyQueryResult(order, queryResult, cancellationToken);
    return PaymentMapper.ToOrderDto(order);
  }

  public async Task<PaymentOrderDto> CloseOrderAsync(
    long id,
    CancellationToken cancellationToken = default)
  {
    var order = await FindOrderAsync(id, cancellationToken);
    if (order.Status is PaymentOrderStatus.Success or PaymentOrderStatus.Refunding
        or PaymentOrderStatus.Refunded or PaymentOrderStatus.PartialRefunded)
    {
      throw new RbacException("当前订单状态不允许关闭");
    }

    order.Status = PaymentOrderStatus.Closed;
    await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);
    return PaymentMapper.ToOrderDto(order);
  }

  public async Task<PaymentRefundDto> RefundAsync(
    CreatePaymentRefundDto input,
    CancellationToken cancellationToken = default)
  {
    var order = await FindOrderAsync(input.PaymentOrderId, cancellationToken);
    if (order.Status is not PaymentOrderStatus.Success and not PaymentOrderStatus.PartialRefunded)
    {
      throw new RbacException("仅支付成功或部分退款的订单可发起退款");
    }

    var refundable = order.Amount - order.RefundedAmount;
    if (refundable <= 0)
    {
      throw new RbacException("订单无可退金额");
    }

    var refundAmount = input.Amount ?? refundable;
    if (refundAmount <= 0 || refundAmount > refundable)
    {
      throw new RbacException("退款金额无效");
    }

    var channel = await FindChannelByCodeAsync(order.ChannelCode, cancellationToken);
    var refund = new PaymentRefund
    {
      RefundNo = PaymentNoGenerator.NewRefundNo(),
      PaymentOrderId = order.Id,
      OrderNo = order.OrderNo,
      ChannelCode = order.ChannelCode,
      Amount = refundAmount,
      TotalAmount = order.Amount,
      Status = PaymentRefundStatus.Pending,
      ChannelOrderNo = order.ChannelOrderNo,
      Reason = NormalizeOptional(input.Reason),
    };

    await _refundRepository.InsertAsync(refund, cancellationToken: cancellationToken);

    order.Status = PaymentOrderStatus.Refunding;
    await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);

    var provider = _providerResolver.Resolve(order.ChannelCode);
    var notifyUrl = _notifyUrlBuilder.BuildRefundNotifyUrl(order.ChannelCode, channel.NotifyUrl);
    var refundResult = await provider.RefundAsync(
      PaymentMapper.ToProviderChannel(channel),
      PaymentMapper.ToProviderOrder(order),
      PaymentMapper.ToProviderRefund(refund),
      notifyUrl,
      cancellationToken);

    if (!refundResult.Success)
    {
      refund.Status = PaymentRefundStatus.Failed;
      refund.ErrorCode = refundResult.ErrorCode;
      refund.ErrorMessage = refundResult.ErrorMessage ?? "退款失败";
      order.Status = order.RefundedAmount > 0
        ? PaymentOrderStatus.PartialRefunded
        : PaymentOrderStatus.Success;
      await _refundRepository.UpdateAsync(refund, cancellationToken: cancellationToken);
      await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);
      throw new RbacException(refund.ErrorMessage);
    }

    refund.Status = refundResult.Status;
    refund.ChannelRefundNo = refundResult.ChannelRefundNo;
    refund.RefundTime = refundResult.RefundTime;

    if (refundResult.Status == PaymentRefundStatus.Success)
    {
      await ApplyRefundSuccess(order, refund, cancellationToken);
    }
    else
    {
      refund.Status = PaymentRefundStatus.Processing;
      await _refundRepository.UpdateAsync(refund, cancellationToken: cancellationToken);
    }

    return PaymentMapper.ToRefundDto(refund);
  }

  internal async Task ApplyQueryResult(
    PaymentOrder order,
    QueryPayResult queryResult,
    CancellationToken cancellationToken)
  {
    if (queryResult.Status == PaymentOrderStatus.Success &&
        order.Status is not PaymentOrderStatus.Success
          and not PaymentOrderStatus.Refunding
          and not PaymentOrderStatus.Refunded
          and not PaymentOrderStatus.PartialRefunded)
    {
      order.Status = PaymentOrderStatus.Success;
      order.PaidTime = queryResult.PaidTime ?? DateTime.UtcNow;
      order.ChannelOrderNo = queryResult.ChannelOrderNo ?? order.ChannelOrderNo;
      order.ErrorCode = null;
      order.ErrorMessage = null;
      await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);
      return;
    }

    if (queryResult.Status == PaymentOrderStatus.Closed &&
        order.Status is PaymentOrderStatus.Paying or PaymentOrderStatus.Pending)
    {
      order.Status = PaymentOrderStatus.Closed;
      await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);
    }
  }

  internal async Task ApplyRefundSuccess(
    PaymentOrder order,
    PaymentRefund refund,
    CancellationToken cancellationToken)
  {
    refund.Status = PaymentRefundStatus.Success;
    refund.RefundTime ??= DateTime.UtcNow;
    order.RefundedAmount += refund.Amount;
    order.Status = order.RefundedAmount >= order.Amount
      ? PaymentOrderStatus.Refunded
      : PaymentOrderStatus.PartialRefunded;

    await _refundRepository.UpdateAsync(refund, cancellationToken: cancellationToken);
    await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);
  }

  private async Task<PaymentOrder> FindOrderAsync(long id, CancellationToken cancellationToken)
  {
    var entity = await _orderRepository.FindAsync(x => x.Id == id, cancellationToken);
    if (entity == null)
    {
      throw new RbacException($"支付订单不存在: {id}");
    }

    return entity;
  }

  private async Task<PaymentChannel> FindChannelByCodeAsync(
    string channelCode,
    CancellationToken cancellationToken)
  {
    var channel = await _channelRepository.GetQueryable()
      .FirstOrDefaultAsync(x => x.ChannelCode == channelCode, cancellationToken);

    if (channel == null)
    {
      throw new RbacException($"支付渠道不存在: {channelCode}");
    }

    return channel;
  }

  private static string? NormalizeOptional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

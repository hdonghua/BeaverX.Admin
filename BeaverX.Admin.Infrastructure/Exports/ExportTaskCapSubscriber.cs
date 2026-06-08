using BeaverX.Admin.Application.Contracts.Exports;
using BeaverX.Admin.Application.Exports;
using BeaverX.Admin.Domain.Shared.Exports;
using BeaverX.Core.Dependency;
using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Infrastructure.Exports;

public class ExportTaskCapSubscriber : IScopedDependency, ICapSubscribe
{
    private readonly ExportTaskMessageService _messageService;
    private readonly ExportTaskExecutor _executor;
    private readonly ILogger<ExportTaskCapSubscriber> _logger;

    public ExportTaskCapSubscriber(
        ExportTaskMessageService messageService,
        ExportTaskExecutor executor,
        ILogger<ExportTaskCapSubscriber> logger)
    {
        _messageService = messageService;
        _executor = executor;
        _logger = logger;
    }

    [CapSubscribe(ExportTaskCapTopics.Execute, Group = ExportTaskCapTopics.ConsumerGroup)]
    public async Task HandleExecuteAsync(ExportTaskExecuteEto message, [FromCap] CapHeader header)
    {
        var capMessageId = header.TryGetValue(Headers.MessageId, out var msgId) && !string.IsNullOrWhiteSpace(msgId)
            ? msgId
            : message.IdempotencyKey;

        _logger.LogInformation(
            "CAP export message received. TaskId={TaskId}, MsgId={MsgId}",
            message.TaskId,
            capMessageId);

        if (!await _messageService.TryClaimForProcessingAsync(message.TaskId, capMessageId))
        {
            return;
        }

        try
        {
            await _executor.ExecuteAsync(message.TaskId);
            await _messageService.MarkConsumedAsync(message.TaskId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CAP export consumer failed. TaskId={TaskId}", message.TaskId);
            await _messageService.ResetForRetryAsync(message.TaskId);
            throw;
        }
    }
}

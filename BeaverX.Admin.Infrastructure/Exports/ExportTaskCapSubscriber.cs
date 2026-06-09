using BeaverX.Admin.Application.Exports;
using BeaverX.Admin.Application.Messaging;
using BeaverX.Admin.Application.Contracts.Exports;
using BeaverX.Admin.Application.Realtime;
using BeaverX.Admin.Domain.Shared.Exports;
using BeaverX.Core.Dependency;
using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Infrastructure.Exports;

public class ExportTaskCapSubscriber : IScopedDependency, ICapSubscribe
{
    private readonly CapMessageConsumeService _capMessageConsumeService;
    private readonly ExportTaskMessageService _messageService;
    private readonly ExportTaskExecutor _executor;
    private readonly RealtimePublisher _realtimePublisher;
    private readonly ILogger<ExportTaskCapSubscriber> _logger;

    public ExportTaskCapSubscriber(
        CapMessageConsumeService capMessageConsumeService,
        ExportTaskMessageService messageService,
        ExportTaskExecutor executor,
        RealtimePublisher realtimePublisher,
        ILogger<ExportTaskCapSubscriber> logger)
    {
        _capMessageConsumeService = capMessageConsumeService;
        _messageService = messageService;
        _executor = executor;
        _realtimePublisher = realtimePublisher;
        _logger = logger;
    }

    [CapSubscribe(ExportTaskCapTopics.Execute, Group = ExportTaskCapTopics.ConsumerGroup)]
    public async Task HandleExecuteAsync(ExportTaskExecuteEto message, [FromCap] CapHeader header)
    {
        if (!header.TryGetValue(Headers.MessageId, out var capMessageId) ||
            string.IsNullOrWhiteSpace(capMessageId))
        {
            _logger.LogWarning("CAP export message missing MessageId. TaskId={TaskId}", message.TaskId);
            return;
        }

        _logger.LogInformation(
            "CAP export message received. TaskId={TaskId}, MsgId={MsgId}",
            message.TaskId,
            capMessageId);

        if (await _capMessageConsumeService.IsConsumedAsync(capMessageId))
        {
            _logger.LogDebug("CAP message {MsgId} already consumed, skip", capMessageId);
            return;
        }

        if (!await _messageService.TryClaimForProcessingAsync(message.TaskId))
        {
            return;
        }

        await _realtimePublisher.NotifyExportTaskChangedByIdAsync(message.TaskId);

        try
        {
            await _executor.ExecuteAsync(message.TaskId);
            await _capMessageConsumeService.MarkConsumedAsync(capMessageId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CAP export consumer failed. TaskId={TaskId}", message.TaskId);
            await _messageService.ResetForRetryAsync(message.TaskId);
            throw;
        }
    }
}

using BeaverX.Admin.Application.Exports;
using BeaverX.Admin.Application.Messaging;
using BeaverX.Admin.Application.Contracts.Exports;
using BeaverX.Admin.Application.Realtime;
using BeaverX.Admin.Domain.Shared.Exports;
using Volo.Abp.DependencyInjection;
using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Infrastructure.Exports;

public class ExportTaskCapSubscriber : ITransientDependency, ICapSubscribe
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExportTaskCapSubscriber> _logger;

    public ExportTaskCapSubscriber(
        IServiceScopeFactory scopeFactory,
        ILogger<ExportTaskCapSubscriber> logger)
    {
        _scopeFactory = scopeFactory;
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

        try
        {
            await _scopeFactory.RunInUnitOfWorkAsync(async (sp, ct) =>
            {
                var capMessageConsumeService = sp.GetRequiredService<CapMessageConsumeService>();
                var messageService = sp.GetRequiredService<ExportTaskMessageService>();
                var executor = sp.GetRequiredService<ExportTaskExecutor>();
                var realtimePublisher = sp.GetRequiredService<RealtimePublisher>();

                if (await capMessageConsumeService.IsConsumedAsync(capMessageId, ct))
                {
                    _logger.LogDebug("CAP message {MsgId} already consumed, skip", capMessageId);
                    return;
                }

                if (!await messageService.TryClaimForProcessingAsync(message.TaskId, ct))
                {
                    return;
                }

                await realtimePublisher.NotifyExportTaskChangedByIdAsync(message.TaskId, ct);
                await executor.ExecuteAsync(message.TaskId, ct);
                await capMessageConsumeService.MarkConsumedAsync(capMessageId, ct);
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CAP export consumer failed. TaskId={TaskId}", message.TaskId);
            await _scopeFactory.RunInUnitOfWorkAsync(async (sp, ct) =>
            {
                await sp.GetRequiredService<ExportTaskMessageService>()
                    .ResetForRetryAsync(message.TaskId, ct);
            });
            throw;
        }
    }
}

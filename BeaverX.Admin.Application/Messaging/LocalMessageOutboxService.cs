using BeaverX.Admin.Domain.Messaging;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Messaging;

public class LocalMessageOutboxService : IScopedDependency
{
    private readonly IRepository<LocalMessageOutbox> _outboxRepository;
    private readonly ILogger<LocalMessageOutboxService> _logger;

    public LocalMessageOutboxService(
        IRepository<LocalMessageOutbox> outboxRepository,
        ILogger<LocalMessageOutboxService> logger)
    {
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public static string BuildIdempotencyKey(string messageType, string businessKey) =>
        $"{messageType}:{businessKey}";

    public async Task<LocalMessageOutbox> CreateAsync(
        string messageType,
        string businessKey,
        string? payload = null,
        CancellationToken cancellationToken = default)
    {
        var outbox = new LocalMessageOutbox
        {
            MessageType = messageType,
            BusinessKey = businessKey,
            IdempotencyKey = BuildIdempotencyKey(messageType, businessKey),
            Payload = payload
        };

        await _outboxRepository.InsertAsync(outbox, cancellationToken: cancellationToken);
        return outbox;
    }

    public async Task<LocalMessageOutbox?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await _outboxRepository.FindAsync(
            x => x.IdempotencyKey == idempotencyKey,
            cancellationToken);
    }

    public async Task MarkPublishedAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var outbox = await GetRequiredAsync(idempotencyKey, cancellationToken);
        if (outbox.IsPublished)
        {
            return;
        }

        outbox.IsPublished = true;
        outbox.PublishedTime = DateTime.UtcNow;
        await _outboxRepository.UpdateAsync(outbox, cancellationToken: cancellationToken);
    }

    public async Task<bool> IsConsumedAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var outbox = await FindByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        return outbox?.IsConsumed == true;
    }

    /// <summary>
    /// 尝试绑定 CAP 消费消息 Id。已消费或已被其他消息占用时返回 false。
    /// </summary>
    public async Task<bool> TryBindConsumeMessageAsync(
        string idempotencyKey,
        string capMessageId,
        CancellationToken cancellationToken = default)
    {
        var outbox = await GetRequiredAsync(idempotencyKey, cancellationToken);
        if (outbox.IsConsumed)
        {
            _logger.LogDebug("Local message {Key} already consumed", idempotencyKey);
            return false;
        }

        if (!string.IsNullOrWhiteSpace(outbox.CapConsumeMessageId))
        {
            return string.Equals(outbox.CapConsumeMessageId, capMessageId, StringComparison.Ordinal);
        }

        outbox.CapConsumeMessageId = capMessageId;
        await _outboxRepository.UpdateAsync(outbox, cancellationToken: cancellationToken);
        return true;
    }

    public async Task ResetConsumeLockAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var outbox = await GetRequiredAsync(idempotencyKey, cancellationToken);
        outbox.CapConsumeMessageId = null;
        await _outboxRepository.UpdateAsync(outbox, cancellationToken: cancellationToken);
    }

    public async Task MarkConsumedAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var outbox = await GetRequiredAsync(idempotencyKey, cancellationToken);
        if (outbox.IsConsumed)
        {
            return;
        }

        outbox.IsConsumed = true;
        outbox.ConsumedTime = DateTime.UtcNow;
        await _outboxRepository.UpdateAsync(outbox, cancellationToken: cancellationToken);
    }

    private async Task<LocalMessageOutbox> GetRequiredAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var outbox = await FindByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (outbox == null)
        {
            throw new InvalidOperationException($"Local message outbox not found: {idempotencyKey}");
        }

        return outbox;
    }
}

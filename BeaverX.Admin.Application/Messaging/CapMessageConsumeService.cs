using BeaverX.Admin.Domain.Messaging;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Messaging;

public class CapMessageConsumeService : IScopedDependency
{
    private readonly IRepository<LocalMessageOutbox> _repository;
    private readonly ILogger<CapMessageConsumeService> _logger;

    public CapMessageConsumeService(
        IRepository<LocalMessageOutbox> repository,
        ILogger<CapMessageConsumeService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> IsConsumedAsync(string capMessageId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(capMessageId))
        {
            return false;
        }

        return await _repository.AnyAsync(x => x.CapMessageId == capMessageId, cancellationToken);
    }

    /// <summary>
    /// 记录 CAP 消息已成功消费。重复调用同一 capMessageId 时幂等忽略。
    /// </summary>
    public async Task MarkConsumedAsync(string capMessageId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(capMessageId))
        {
            throw new ArgumentException("CAP message id is required.", nameof(capMessageId));
        }

        if (await IsConsumedAsync(capMessageId, cancellationToken))
        {
            _logger.LogDebug("CAP message {CapMessageId} already marked consumed", capMessageId);
            return;
        }

        try
        {
            await _repository.InsertAsync(
                new LocalMessageOutbox
                {
                    CapMessageId = capMessageId,
                    ConsumedTime = DateTime.UtcNow
                },
                cancellationToken: cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            if (await IsConsumedAsync(capMessageId, cancellationToken))
            {
                _logger.LogDebug(ex, "CAP message {CapMessageId} consumed record inserted concurrently", capMessageId);
                return;
            }

            throw;
        }
    }
}

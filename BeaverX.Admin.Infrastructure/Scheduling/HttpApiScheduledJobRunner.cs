using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BeaverX.Admin.Domain.Scheduling;
using BeaverX.Admin.Domain.Shared.Scheduling;
using BeaverX.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Infrastructure.Scheduling;

public class HttpApiScheduledJobRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpApiScheduledJobRunner> _logger;

    public HttpApiScheduledJobRunner(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<HttpApiScheduledJobRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(long jobId, bool isManualTrigger, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IRepository<ScheduledJob>>();
        var logRepository = scope.ServiceProvider.GetRequiredService<IRepository<ScheduledJobLog>>();

        var job = await jobRepository.FindAsync(x => x.Id == jobId, cancellationToken);
        if (job == null)
        {
            _logger.LogWarning("Scheduled job {JobId} not found", jobId);
            return;
        }

        if (!job.IsEnabled && !isManualTrigger)
        {
            _logger.LogInformation("Scheduled job {JobId} is disabled, skip execution", jobId);
            return;
        }

        if (job.JobType != ScheduledJobType.HttpApi)
        {
            await WriteFailureAsync(jobRepository, logRepository, job, isManualTrigger, "不支持的任务类型", cancellationToken);
            return;
        }

        var startedAt = DateTime.UtcNow;
        var log = new ScheduledJobLog
        {
            JobId = job.Id,
            StartedAt = startedAt,
            IsManualTrigger = isManualTrigger
        };

        try
        {
            using var client = _httpClientFactory.CreateClient(nameof(HttpApiScheduledJobRunner));
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(job.TimeoutSeconds, 1, 600));

            using var request = BuildRequest(job);
            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var truncatedBody = Truncate(responseBody, 4000);

            log.FinishedAt = DateTime.UtcNow;
            log.DurationMs = (int)Math.Max(0, (log.FinishedAt.Value - startedAt).TotalMilliseconds);
            log.HttpStatusCode = (int)response.StatusCode;
            log.ResponseBody = truncatedBody;
            log.Status = response.IsSuccessStatusCode
                ? ScheduledJobRunStatus.Success
                : ScheduledJobRunStatus.Failed;
            log.ErrorMessage = response.IsSuccessStatusCode
                ? null
                : $"HTTP {(int)response.StatusCode}";

            job.LastRunTime = log.FinishedAt;
            job.LastRunStatus = log.Status;
            job.LastRunMessage = log.Status == ScheduledJobRunStatus.Success
                ? $"HTTP {(int)response.StatusCode}"
                : log.ErrorMessage;
        }
        catch (Exception ex)
        {
            log.FinishedAt = DateTime.UtcNow;
            log.DurationMs = (int)Math.Max(0, (log.FinishedAt.Value - startedAt).TotalMilliseconds);
            log.Status = ScheduledJobRunStatus.Failed;
            log.ErrorMessage = Truncate(ex.Message, 1024);

            job.LastRunTime = log.FinishedAt;
            job.LastRunStatus = ScheduledJobRunStatus.Failed;
            job.LastRunMessage = log.ErrorMessage;

            _logger.LogError(ex, "Scheduled job {JobId} execution failed", jobId);
        }

        await logRepository.InsertAsync(log, cancellationToken: cancellationToken);
        await jobRepository.UpdateAsync(job, cancellationToken: cancellationToken);
    }

    private static HttpRequestMessage BuildRequest(ScheduledJob job)
    {
        var method = job.HttpMethod switch
        {
            ScheduledJobHttpMethod.Post => HttpMethod.Post,
            ScheduledJobHttpMethod.Put => HttpMethod.Put,
            ScheduledJobHttpMethod.Delete => HttpMethod.Delete,
            _ => HttpMethod.Get
        };

        var request = new HttpRequestMessage(method, job.HttpUrl);
        ApplyHeaders(request, job.HttpHeadersJson);

        if (method == HttpMethod.Post || method == HttpMethod.Put)
        {
            var body = job.HttpBody ?? string.Empty;
            request.Content = new StringContent(body, Encoding.UTF8);
            if (request.Content.Headers.ContentType == null)
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }
        }

        return request;
    }

    private static void ApplyHeaders(HttpRequestMessage request, string? headersJson)
    {
        if (string.IsNullOrWhiteSpace(headersJson))
        {
            return;
        }

        Dictionary<string, string>? headers;
        try
        {
            headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"请求头 JSON 无效: {ex.Message}");
        }

        if (headers == null)
        {
            return;
        }

        foreach (var (key, value) in headers)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!request.Headers.TryAddWithoutValidation(key, value))
            {
                request.Content ??= new StringContent(string.Empty);
                request.Content.Headers.TryAddWithoutValidation(key, value);
            }
        }
    }

    private static async Task WriteFailureAsync(
        IRepository<ScheduledJob> jobRepository,
        IRepository<ScheduledJobLog> logRepository,
        ScheduledJob job,
        bool isManualTrigger,
        string message,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        job.LastRunTime = now;
        job.LastRunStatus = ScheduledJobRunStatus.Failed;
        job.LastRunMessage = message;

        await logRepository.InsertAsync(new ScheduledJobLog
        {
            JobId = job.Id,
            StartedAt = now,
            FinishedAt = now,
            DurationMs = 0,
            Status = ScheduledJobRunStatus.Failed,
            ErrorMessage = message,
            IsManualTrigger = isManualTrigger
        }, cancellationToken: cancellationToken);

        await jobRepository.UpdateAsync(job, cancellationToken: cancellationToken);
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value ?? string.Empty;
        }

        return value[..maxLength];
    }
}

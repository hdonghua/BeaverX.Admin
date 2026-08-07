using BeaverX.Admin.Application.Contracts.Oa;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Oa;

internal class TestOaServiceTaskHandler : IOaServiceTaskHandler
{
    private readonly ILogger<TestOaServiceTaskHandler> _logger;

    public TestOaServiceTaskHandler(ILogger<TestOaServiceTaskHandler> logger)
    {
        _logger = logger;
    }

    public string Key => "Test";

    public string DisplayName => "测试任务";

    public async Task HandleAsync(OaServiceTaskContext context, CancellationToken cancellationToken = default)
    {
        if (context.FormData.TryGetValue("num", out var numObj) && (int?)numObj >= 99)
        {
            _logger.LogWarning("数量超过99了");
        }
    }
}

using BeaverX.Admin.Application.Contracts.Payment;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Infrastructure.Payment;

public class PaymentOptionsPostConfigure : IConfigureOptions<PaymentOptions>
{
    private readonly IHostEnvironment _environment;

    public PaymentOptionsPostConfigure(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public void Configure(PaymentOptions options)
    {
        if (Path.IsPathRooted(options.CertCacheRootPath))
        {
            return;
        }

        options.CertCacheRootPath = Path.Combine(
          _environment.ContentRootPath,
          options.CertCacheRootPath);
    }
}

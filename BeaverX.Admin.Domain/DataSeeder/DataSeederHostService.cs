using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeaverX.Admin.Domain.DataSeeder
{
    internal class DataSeederHostService : IHostedService
    {
        private readonly IServiceProvider serviceProvider;

        public DataSeederHostService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();
            var seeders = scope.ServiceProvider.GetService<IEnumerable<IDataSeeder>>();
            if (seeders != null)
            {
                foreach (var seeder in seeders)
                {
                    await seeder.SeedAsync(cancellationToken);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

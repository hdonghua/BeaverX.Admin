using System.Reflection;
using BeaverX.Domain.Entities;
using BeaverX.Domain.Repositories;
using BeaverX.Admin.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BeaverX.Admin.EntityFrameworkCore;

internal static class AdminRepositoryRegistrationExtensions
{
    public static IServiceCollection ReplaceWithAdminRepositories(this IServiceCollection services)
    {
        var dbSetProperties = typeof(AdminDbContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

        foreach (var prop in dbSetProperties)
        {
            var entityType = prop.PropertyType.GetGenericArguments()[0];
            var entityInterface = entityType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));

            if (entityInterface is null)
            {
                continue;
            }

            var keyType = entityInterface.GetGenericArguments()[0];
            if (keyType != typeof(long))
            {
                continue;
            }

            var adminRepoType = typeof(AdminEfCoreRepository<>).MakeGenericType(entityType);
            var repositoryInterface = typeof(IRepository<>).MakeGenericType(entityType);
            var repositoryWithKeyInterface = typeof(IRepository<,>).MakeGenericType(entityType, keyType);

            services.Replace(ServiceDescriptor.Scoped(repositoryInterface, adminRepoType));
            services.Replace(ServiceDescriptor.Scoped(repositoryWithKeyInterface, adminRepoType));
        }

        return services;
    }
}

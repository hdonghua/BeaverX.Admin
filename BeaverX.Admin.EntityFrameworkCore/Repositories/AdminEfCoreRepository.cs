using BeaverX.Domain.Entities;
using BeaverX.Domain.Repositories;
using BeaverX.EntityFrameworkCore.Repositories;

namespace BeaverX.Admin.EntityFrameworkCore.Repositories;

public class AdminEfCoreRepository<TEntity> : EfCoreRepository<AdminDbContext, TEntity>, IRepository<TEntity>
    where TEntity : class, IEntity<long>
{
    private readonly IEntityIdGenerator _entityIdGenerator;

    public AdminEfCoreRepository(AdminDbContext dbContext, IEntityIdGenerator entityIdGenerator)
        : base(dbContext)
    {
        _entityIdGenerator = entityIdGenerator;
    }

    public override async Task<TEntity> InsertAsync(
        TEntity entity,
        bool autoSave = true,
        CancellationToken cancellationToken = default)
    {
        AssignIdIfNeeded(entity);
        return await base.InsertAsync(entity, autoSave, cancellationToken);
    }

    public override async Task InsertManyAsync(
        IEnumerable<TEntity> entities,
        bool autoSave = true,
        CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            AssignIdIfNeeded(entity);
        }

        await base.InsertManyAsync(entities, autoSave, cancellationToken);
    }

    private void AssignIdIfNeeded(TEntity entity)
    {
        if (entity.Id == 0)
        {
            entity.Id = _entityIdGenerator.CreateId();
        }
    }
}

using IdGen;

namespace BeaverX.Admin.EntityFrameworkCore;

public sealed class SnowflakeEntityIdGenerator : IEntityIdGenerator
{
    private readonly IdGenerator _idGenerator;

    public SnowflakeEntityIdGenerator(IdGenerator idGenerator)
    {
        _idGenerator = idGenerator;
    }

    public long CreateId() => _idGenerator.CreateId();
}

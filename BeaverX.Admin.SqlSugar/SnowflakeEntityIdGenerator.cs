using BeaverX.Domain.IdGeneration;
using IdGenerator = IdGen.IdGenerator;

namespace BeaverX.Admin.EntityFrameworkCore;

public sealed class SnowflakeEntityIdGenerator : IIdGenerator<long>
{
    private readonly IdGenerator _idGenerator;

    public SnowflakeEntityIdGenerator(IdGenerator idGenerator)
    {
        _idGenerator = idGenerator;
    }

    public long Generate() => _idGenerator.CreateId();
}

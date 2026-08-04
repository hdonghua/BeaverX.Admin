using Volo.Abp.DependencyInjection;

namespace BeaverX.Admin.Application.Oa;

public class OaIdGenerator : ISingletonDependency
{
    public Guid Create() => Guid.NewGuid();
}

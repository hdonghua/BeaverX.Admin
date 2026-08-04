using BeaverX.Admin.Domain.Oa;
using BeaverX.Admin.Domain.Rbac;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace BeaverX.Admin.Application.Oa;

public class OaDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<OaDepartment, Guid> _departments;
    private readonly IRepository<OaUserDepartment, Guid> _userDepartments;
    private readonly IRepository<User, Guid> _users;
    private readonly IGuidGenerator _ids;

    public OaDataSeeder(
        IRepository<OaDepartment, Guid> departments,
        IRepository<OaUserDepartment, Guid> userDepartments,
        IRepository<User, Guid> users,
        IGuidGenerator ids)
    {
        _departments = departments;
        _userDepartments = userDepartments;
        _users = users;
        _ids = ids;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        var department = await (await _departments.GetQueryableAsync()).FirstOrDefaultAsync();
        if (department == null)
        {
            var adminId = await (await _users.GetQueryableAsync()).Where(x => x.UserName == "admin").Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
            department = new OaDepartment(_ids.Create())
            {
                Name = "默认部门", Code = "DEFAULT", LeaderUserId = adminId, Sort = 0, IsEnabled = true
            };
            await _departments.InsertAsync(department, autoSave: true);
        }

        var linkedUsers = await (await _userDepartments.GetQueryableAsync()).Select(x => x.UserId).ToListAsync();
        var users = await (await _users.GetQueryableAsync()).Where(x => x.IsEnabled && !linkedUsers.Contains(x.Id)).Select(x => x.Id).ToListAsync();
        if (users.Count > 0)
        {
            await _userDepartments.InsertManyAsync(users.Select(userId => new OaUserDepartment(_ids.Create())
            {
                UserId = userId, DepartmentId = department.Id, IsPrimary = true
            }), autoSave: true);
        }
    }
}

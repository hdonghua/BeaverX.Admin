using BeaverX.Domain.Users;
using BeaverX.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.EntityFrameworkCore
{
    internal class AdminDbContext : BeaverXDbContext<AdminDbContext>
    {
        public AdminDbContext(DbContextOptions<AdminDbContext> options, ICurrentUser currentUser) : base(options, currentUser)
        {
        }
    }
}

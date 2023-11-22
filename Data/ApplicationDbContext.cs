using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Task = Optimizer.Models.Domain.Task;

namespace Optimizer.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        //ToDo: Consider confugring auth settings
        //protected override void OnModelCreating(ModelBuilder builder)
        //{
            //configure the schema needed for this auth context 
            //base.OnModelCreating(builder);
            //seed roles
            //var userRoleId = "abdcf0fa-4dd6-4e9f-a6d0-67cae7eb8e02";
            //var superAdminRoleId = "4515ca9e-0e3a-49f6-8679-3161268387e4";
        //}
    }
}
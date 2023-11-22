using Microsoft.EntityFrameworkCore;
using Optimizer.Models.Domain;
using System.Reflection.Emit;
using Task = Optimizer.Models.Domain.Task;

namespace Optimizer.Data
{
    public class TaskContext : DbContext
    {
        public TaskContext(DbContextOptions<TaskContext> options) : base(options) {
            
        }

        public DbSet<Task> Tasks { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Status> Statuses { get; set; } = null!;

        //This function feeds preset data into the database
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasData(
                    new Category { CategoryId = "work", Name = "Work" },
                    new Category { CategoryId = "home", Name = "Home" },
                    new Category { CategoryId = "ex", Name = "Excercise" },
                    new Category { CategoryId = "shop", Name = "Shopping" },
                    new Category { CategoryId = "call", Name = "Contact" }
                );

            modelBuilder.Entity<Status>().HasData(
                    new Status { StatusId ="open", Name="Open"},
                    new Status { StatusId ="closed", Name="Completed"}
                );

        }
    }
}

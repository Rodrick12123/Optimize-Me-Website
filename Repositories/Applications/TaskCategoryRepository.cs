using Microsoft.EntityFrameworkCore;
using Optimizer.Data;
using Optimizer.Models.Domain;
using Optimizer.Repositories.Interfaces;

namespace Optimizer.Repositories.Applications
{
    public class TaskCategoryRepository : ITaskCategoryRepository
    {
        private readonly TaskContext context;

        public TaskCategoryRepository(TaskContext context )
        {
            this.context = context;
        }
        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            var categories = await context.Categories.ToListAsync();
            return categories;
        }
    }
}

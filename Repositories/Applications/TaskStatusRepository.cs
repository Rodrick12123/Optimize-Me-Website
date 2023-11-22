using Microsoft.EntityFrameworkCore;
using Optimizer.Data;
using Optimizer.Models.Domain;
using Optimizer.Repositories.Interfaces;

namespace Optimizer.Repositories.Applications
{
    public class TaskStatusRepository : ITaskStatusesRepository
    {
        private readonly TaskContext context;
        public TaskStatusRepository(TaskContext context)
        {
            this.context = context;
        }
        public async Task<IEnumerable<Status>> GetAllStatuesAsync()
        {
            var statuses = await context.Statuses.ToListAsync();
            return statuses;
        }
    }
}

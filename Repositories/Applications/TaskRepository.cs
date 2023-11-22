using Microsoft.EntityFrameworkCore;
using Optimizer.Data;
using Optimizer.Models.Domain;
using Optimizer.Repositories.Interfaces;
using DomainTask = Optimizer.Models.Domain.Task;
using Microsoft.AspNetCore.Identity;


namespace Optimizer.Repositories.Applications
{
    
    public class TaskRepository : ITaskRepository
    {
        private readonly TaskContext context;

        public TaskRepository(TaskContext context)
        {
            this.context = context;
        }
        public async Task<DomainTask> AddAsync(DomainTask task)
        {
            await context.Tasks.AddAsync(task);
            await context.SaveChangesAsync();
            return task;
        }

        public IQueryable<DomainTask> GetAllUserTask(string userId)
        {
            var userTask = context.Tasks.Where(t => t.UserId == userId)
                .Include(t => t.Status)
                .Include(t => t.Category); 
            return userTask;
        }

        public async Task<DomainTask> CloseAsync(DomainTask task)
        {
            task.StatusId = "closed";
            context.SaveChanges();
            return task;
        }

        public async Task<IEnumerable<DomainTask>> DeleteCompleteAsync()
        {
            var deletedTasks = await context.Tasks.Where(t => t.StatusId == "closed").ToListAsync();
            foreach (var task in deletedTasks)
            {
                context.Tasks.Remove(task);
            }
            await context.SaveChangesAsync();
            return deletedTasks;

        }


        public IQueryable<DomainTask> GetAllFilteredTask(Filters filters, IQueryable<DomainTask>? query=null)
        {
            //Query Db table with task info 
            if(query == null)
            {
                query = context.Tasks
                    .Include(t => t.Status)
                    .Include(t => t.Category);
            }
            

            //Check for filter values
            //Then update queries with WHERE 

            if (filters.HasCategory)
            {
                query = query.Where(t => t.CategoryId == filters.CategoryId);
            }
            if (filters.HasDue)
            {
                var currentDate = DateTime.Today;
                if (filters.IsFuture)
                {
                    query = query.Where(t => t.DueDate > currentDate);
                }

                else if (filters.IsPast)
                {
                    query = query.Where(t => t.DueDate < currentDate);
                }
                else if (filters.IsToday)
                {
                    query = query.Where(t => t.DueDate == currentDate);
                }
            }
            if (filters.HasStatus)
            {
                query = query.Where(t => t.StatusId == filters.StatusId);
            }
            return query;
        }


        public async Task<DomainTask> GetAsync(int id)
        {
            var task = await context.Tasks.FindAsync(id);
            return task;
        }

        public async Task<IEnumerable<DomainTask>> OrderTaskByDueDate(IQueryable<DomainTask> query)
        {
            var tasks = await query.OrderBy(t => t.DueDate).ToListAsync();
            return tasks;
        }
    }
}

using Optimizer.Models.Domain;
using Task = Optimizer.Models.Domain.Task;

namespace Optimizer.Repositories.Interfaces
{
    public interface ITaskRepository
    {
        IQueryable<Task> GetAllFilteredTask(Filters filters, IQueryable<Task>? query = null);
        Task<IEnumerable<Task>> OrderTaskByDueDate(IQueryable<Task> query);
        Task<Task> AddAsync(Task task);
        IQueryable<Task> GetAllUserTask(string userId);
        Task<Task> GetAsync(int id);
        Task<Task> CloseAsync(Task task);
        Task<IEnumerable<Task>> DeleteCompleteAsync();
    }
}
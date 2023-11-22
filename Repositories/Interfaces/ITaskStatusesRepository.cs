using Optimizer.Models.Domain;

namespace Optimizer.Repositories.Interfaces
{
    public interface ITaskStatusesRepository
    {
        Task<IEnumerable<Status>> GetAllStatuesAsync();
    }
}

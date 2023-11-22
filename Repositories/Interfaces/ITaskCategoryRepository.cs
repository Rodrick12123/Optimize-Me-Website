using Optimizer.Models.Domain;

namespace Optimizer.Repositories.Interfaces
{
    public interface ITaskCategoryRepository
    {
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
    }
}

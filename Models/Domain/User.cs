using Microsoft.AspNetCore.Identity;

namespace Optimizer.Models.Domain
{
    public class User: IdentityUser
    {
        public ICollection<Task> Tasks { get; set; }
    }
}

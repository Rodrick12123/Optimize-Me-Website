using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Optimizer.Models.Domain
{
    public class Task
    {
        public int Id { get; set; }

        public List<string> ListOfAttributes { get; } = ["Description", "Category", "Due Date", "Status"];

        [Required(ErrorMessage = "Please enter a description for your task.")]
        public string Description { get; set; } = string.Empty;

        public int Value { get; set; } = 1;

        [Required(ErrorMessage = "Please enter a due date.")]
        public DateTime? DueDate { get; set; }

        public int Time { get; set; } = 0;

        //1 to multi relational DB
        [Required(ErrorMessage = "Please choose a category.")]
        public string CategoryId { get; set; } = string.Empty;
        [ValidateNever]
        public Category Category { get; set; } = null!;

        //1 to multi relational DB
        [Required(ErrorMessage = "Please choose a status.")]
        public string StatusId { get; set; } = string.Empty;
        [ValidateNever]
        public Status Status { get; set; } = null!;

        public bool Overdue => StatusId == "open" && DueDate < DateTime.Today;

        //Foreign Key to UserId
        [ValidateNever]
        public string UserId { get; set; }
        
    }
}

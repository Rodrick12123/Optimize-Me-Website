namespace Optimizer.Models
{
    public class Tasks
    {
        private int Id { get; set; }

        public List<Task> tasks { get; set; } = null!;

        public int TotalTaskTime { get; set; }

        public int MaxTime { get; set; } 

        public bool OverTimeLimit => TotalTaskTime > MaxTime;

        //Function for sorting all of the task by getting the most value 
        //for the maximum time allotted (If MaxTime is declared)
    }
}

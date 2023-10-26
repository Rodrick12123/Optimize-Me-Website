namespace Optimizer.Models
{
    //Class for sorting and filtering data
    public class Filters
    {
        public string Filter { get; }

        public string CategoryId { get; }

        public string Due { get; }

        public string StatusId { get; }

        public int? MaxTime { get; }

        public bool HasCategory => CategoryId.ToLower() != "all";
        public bool HasDue => Due.ToLower() != "all";
        public bool HasStatus => StatusId.ToLower() != "all";
        public bool HasMaxTime => MaxTime != null;

        public Filters(string filter, int? maxtime = null )
        {


            Filter = filter ?? "all-all-all";
            String[] filters = Filter.Split("-");

            CategoryId = filters[0];
            Due = filters[1];
            StatusId = filters[2];

            //TotalTime = totaltime ?? 0;
            if (maxtime >= 0)
            {
                MaxTime = maxtime;
            }


        }

        
        public static Dictionary<string, string> DueFilterValues =>
            new Dictionary<string, string>
            {
                {"future", "Future" },
                {"past", "Past" },
                {"today", "Today" }
            };
        public bool IsPast => Due.ToLower() == "past";
        public bool IsFuture => Due.ToLower() == "future";
        public bool IsToday => Due.ToLower() == "today";
    }
}

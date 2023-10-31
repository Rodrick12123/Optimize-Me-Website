using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using Newtonsoft.Json.Linq;
using Optimizer.Models;
using System.CodeDom;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using static NuGet.Packaging.PackagingConstants;
using Task = Optimizer.Models.Task;

namespace Optimizer.Controllers
{
    public class TaskController : Controller
    {
        private TaskContext context;

        private readonly ILogger<TaskController> _logger;

        //REMEMBER TO INCLUDE ASYNCRONOUS PROGRAMMING
        public TaskController(TaskContext contxt, ILogger<TaskController> logger)
        {
            
            context = contxt;
            _logger = logger;
        }

        public IActionResult Index(string id, int? time = null)
        {
            //Set new filter object with filters represented by strings
            //Include the MaxTime parameter
            var filters = new Filters(id);

            //add elements to view bag
            ViewBag.Filters = filters;
            ViewBag.Categories = context.Categories.ToList();
            ViewBag.DueFilters = Filters.DueFilterValues;
            ViewBag.Statuses = context.Statuses.ToList();

            //Query Db table with task info 
            IQueryable<Task> query = context.Tasks
                .Include(t => t.Status)
                .Include(t => t.Category);

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
            //MaxTime filter here

            //Execute query and order by DueDate
            //Consider having mutiple order options
            var tasks = query.OrderBy(t => t.DueDate).ToList();
            
            return View(tasks);
        }

        //Get method for adding information
        [HttpGet]
        public IActionResult Add()
        {
            ViewBag.Categories = context.Categories.ToList();
            ViewBag.Statuses = context.Statuses.ToList();
            var task = new Task { StatusId = "open" };
            return View(task);

        }

        //Post method for adding information
        [HttpPost]
        public IActionResult Add(Task task)
        {
            //Check for valid inputs and adds task to the database
            //then saves the database change
            //If the input is invalid return the current view and reload bag
            if (ModelState.IsValid)
            {
                context.Tasks.Add(task);
                context.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.Categories = context.Categories.ToList();
                ViewBag.Statuses = context.Statuses.ToList();
                return View(task);
            }
        }

        //need to add maxtime
        [HttpPost]
        public IActionResult Filter(string[] filter, int? time = null)
        {
            string id = string.Join("-", filter);
            return RedirectToAction("Index", new { ID = id });
        }

        //Changes the StatusId of the task within the database to closed
        [HttpPost]
        public IActionResult MarkComplete([FromRoute] string id, Task task)
        {
            task = context.Tasks.Find(task.Id);

            if (task != null)
            {
                task.StatusId = "closed";
                context.SaveChanges();
            }
            return RedirectToAction("Index", new { ID = id });

        }

        //Delete all of the completed task
        [HttpPost]
        public IActionResult DeleteComplete(string id)
        {
            //query that holds all of the completed task
            var deleteTask = context.Tasks.Where(t => t.StatusId == "closed").ToList();

            foreach (var task in deleteTask)
            {
                context.Tasks.Remove(task);
            }
            context.SaveChanges();
            return RedirectToAction("Index", new { ID = id });

        }

        [HttpPost]
        public IActionResult Optimize(string id, int time = 0)
        {
            //Query Db table with task info 
            IQueryable<Task> query = context.Tasks
                .Include(t => t.Status)
                .Include(t => t.Category);
            List<Task> currentTask = query.OrderBy(t => t.DueDate).ToList();

            //filter the task with the appropriate ids
            List<int> acceptableIds = OptimizerHelper(time, currentTask);

            var filteredItems = currentTask.Where(item => acceptableIds.Contains(item.Id)).ToList();
            //return the optimized tasks to view
            
            return RedirectToAction("Index", filteredItems);

        }


        //Returns a list of task ids that offer the most value within the constraints of alloted time
        public List<int> OptimizerHelper(int maxtime, List<Task> tasks, int value = 0, int maxvalue = 0, List<int> listOfId = null)
        {
            //base case

            if (tasks.Count == 0 || maxtime <= 0 )
            {
                return [];
            }

            foreach (Task task in tasks)
            {

                if ((maxtime - task.Time) < 0)
                {

                    continue;
                }
                value = value + task.Value;
                maxtime = maxtime - task.Time;
                List<Task> tempList = tasks;
                tempList.Remove(task);
                
                listOfId = OptimizerHelper(maxtime, tempList, value, maxvalue, listOfId);

                if (maxvalue < value)
                {
                    maxvalue = value;
                    int taskId = task.Id;
                    listOfId.Add(taskId);
                    return listOfId;
                }
            }

            return listOfId;
        }

        //Include another filter function that allows optimizing based off of the due date
        //and spending the least amount of time for the highest value

        //function for testing the dp used for optimization algorithms
        public string Test1(int maxtime, Dictionary<string, List<int>> tasksList, int value = 0, int maxvalue = 0, string beststring = "")
        {
            //base case
            
            if (tasksList.Count == 0 || maxtime <= 0)
            {
                return "";
            }

            foreach (string key in tasksList.Keys)
            {
                
                if ((maxtime - tasksList[key][1]) < 0)
                {
                    
                    continue;
                }
                value = value + tasksList[key][0];
                maxtime = maxtime - tasksList[key][1];
                Dictionary<string, List<int>> tempList = tasksList;
                tempList.Remove(key);
                beststring = Test1(maxtime, tempList, value, maxvalue, beststring);

                if (maxvalue < value)
                {
                    maxvalue = value;
                    return beststring += key;
                }
            }

            return beststring;
        }

        public List<int> Test2(int maxtime, List<Task> tasks, int value = 0, int maxvalue = 0, List<int> listOfId = null)
        {
            //base case

            if (tasks.Count == 0 || maxtime <= 0)
            {
                return [];
            }

            foreach (Task task in tasks)
            {

                if ((maxtime - task.Time) < 0)
                {

                    continue;
                }
                value = value + task.Value;
                maxtime = maxtime - task.Time;
                List<Task> tempList = tasks;
                tempList.Remove(task);

                listOfId = OptimizerHelper(maxtime, tempList, value, maxvalue, listOfId);

                if (maxvalue < value)
                {
                    maxvalue = value;
                    int taskId = task.Id;
                    listOfId.Add(taskId);
                    return listOfId;
                }
            }

            return listOfId;
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}


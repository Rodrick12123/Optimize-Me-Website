using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using Newtonsoft.Json.Linq;
using NuGet.Packaging;
using Optimizer.Models;
using System.CodeDom;
using System.Collections.Generic;
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
            var currentTask = query.OrderBy(t => t.DueDate).ToList();
           
            //filter the task with the appropriate ids
            List<Task> acceptableTasks = OptimizerHelper(time, currentTask, currentTask.Count);
            
            
            //return the optimized tasks to optimize view
            
            return View(acceptableTasks);

        }


        

        //Include another filter function that allows optimizing based off of the due date
        //and spending the least amount of time for the highest value

        //function for testing the dp used for optimization algorithms (working)
        //CONSIDER MEMORY OPTIMIZATION
        public string Test1(int maxtime, Dictionary<string, List<int>> tasksList, int value = 0, int maxvalue = 0, string beststring = "")
        {
            //base case
            
            
            if (tasksList.Count == 0)
            {
                return "";
            }
            if (maxtime == 0)
            {
                return "";
            }
            if(maxtime < 0)
            {
                return null;
            }


            
            foreach (string key in tasksList.Keys)
            {
                
                int tempValue = value + tasksList[key][0];
                int tempMaxTime = maxtime - tasksList[key][1];
                if (tempMaxTime < 0)
                {
                    continue;
                }
                Dictionary<string, List<int>> tempList = new Dictionary<string, List<int>>(tasksList);
                tempList.Remove(key);
                string possible = key + Test1(tempMaxTime, tempList, tempValue, maxvalue, beststring);

                if (maxvalue < tempValue)
                {
                   
                    maxvalue = tempValue;
                    
                    beststring = possible;
                }
                

            }

            return beststring;

        }

        //Helper function for the optimize function
        //returns a list of task where the total amount of time < maxtime and
        //Contains the largest value out of all valid combinations within maxtime
        public List<Task> OptimizerHelper(int maxtime, List<Task> tasks, int index = 0)
        {
            //base case
            if (index == 0 || maxtime == 0)
            {
                return [];
            }
            //skip condition
            if (tasks[index - 1].Time > maxtime)
            {
                return OptimizerHelper(maxtime, tasks, index - 1);
            }
            //recursive calls
            List<Task> excludeTasks = OptimizerHelper(maxtime, tasks, index - 1);
            List<Task> includeTasks = OptimizerHelper(maxtime - tasks[index - 1].Time, tasks, index - 1);
            //add Task to the lists
            includeTasks.Add(tasks[index - 1]);
            //sum up all of the values within both lists
            int excludeTotal = excludeTasks.Sum(item => item.Value);
            int includeTotal = includeTasks.Sum(item => item.Value);
            //choose which list choices results in the highest total value
            return includeTotal > excludeTotal ? includeTasks : excludeTasks;
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


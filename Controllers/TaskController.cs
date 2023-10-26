using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Optimizer.Models;
using System.CodeDom;
using System.Diagnostics;
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
            //_logger = logger;
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

        
        public IActionResult Optimize(string id, int? time = null)
        {
            //Query Db table with task info 
            //IQueryable<Task> query = context.Tasks
            //    .Include(t => t.Status)
            //    .Include(t => t.Category);


            //DP for calculating optimized list of Task

            //return the optimized tasks to view
            return RedirectToAction("Index");

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


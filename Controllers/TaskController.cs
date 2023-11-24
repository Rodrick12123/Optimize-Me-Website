using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
using Optimizer.Data;
using Optimizer.Models.Domain;
using Optimizer.Repositories.Interfaces;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static NuGet.Packaging.PackagingConstants;
using Task = Optimizer.Models.Domain.Task;

namespace Optimizer.Controllers
{
    [Authorize]
    public class TaskController : Controller
    {

        private readonly ITaskRepository taskRepository;
        private readonly ITaskCategoryRepository categoryRepo;
        private readonly ITaskStatusesRepository statusRepo;
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> loginManager;

        //REMEMBER TO INCLUDE ASYNCRONOUS PROGRAMMING
        public TaskController(ITaskRepository taskRepository,SignInManager<IdentityUser> loginManager, ITaskCategoryRepository categoryRepo,
            ITaskStatusesRepository statusRepo, UserManager<IdentityUser> userManager)
        {

            this.loginManager = loginManager;
            this.taskRepository = taskRepository;
            this.categoryRepo = categoryRepo;
            this.statusRepo = statusRepo;
            this.userManager = userManager;
        }

        public async Task<IActionResult> Index(string id, int? time = null)
        {
            //Set new filter object with filters represented by strings
            //Include the MaxTime parameter
            var filters = new Filters(id);

            //add elements to view bag
            ViewBag.Filters = filters;
            ViewBag.Categories = await categoryRepo.GetAllCategoriesAsync();
            ViewBag.DueFilters = Filters.DueFilterValues;
            ViewBag.Statuses = await statusRepo.GetAllStatuesAsync();

            //Fetches only the task a user has created
            if (loginManager.IsSignedIn(User))
            {
                var userTaskQuery = taskRepository.GetAllUserTask(userManager.GetUserId(User));
                var query = taskRepository.GetAllFilteredTask(filters, userTaskQuery);

                //Execute query and order by DueDate
                //Consider having mutiple order options
                var tasks = await taskRepository.OrderTaskByDueDate(query);

                return View(tasks);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
            
        }

        //Get method for adding information
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            ViewBag.Categories = await categoryRepo.GetAllCategoriesAsync();
            ViewBag.Statuses = await statusRepo.GetAllStatuesAsync();
            var task = new Task { StatusId = "open" };
            return View(task);

        }

        //Post method for adding information
        [HttpPost]
        public async Task<IActionResult> Add(Task task)
        {
            //Check for valid inputs and adds task to the database
            //then saves the database change
            //If the input is invalid return the current view and reload bag
            if (ModelState.IsValid)
            {
                if (loginManager.IsSignedIn(User))
                {
                    task.UserId = userManager.GetUserId(User);
                }
                await taskRepository.AddAsync(task);
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.Categories = await categoryRepo.GetAllCategoriesAsync();
                ViewBag.Statuses = await statusRepo.GetAllStatuesAsync();
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
        public async Task<IActionResult> MarkComplete([FromRoute] string id, Task task)
        {
            task = await taskRepository.GetAsync(task.Id);

            if (task != null)
            {
                await taskRepository.CloseAsync(task);
            }
            return RedirectToAction("Index", new { ID = id });

        }

        //Delete all of the completed task
        [HttpPost]
        public async Task<IActionResult> DeleteComplete(string id)
        {
            //query that holds all of the completed task
            var deleteTask = await taskRepository.DeleteCompleteAsync();

            
            return RedirectToAction("Index", new { ID = id });

        }

        [HttpPost]
        public async Task<IActionResult> Optimize(string id, int time = 0)
        {
            var filters = new Filters(id);

            //add elements to view bag
            ViewBag.Filters = filters;
            ViewBag.Categories = await categoryRepo.GetAllCategoriesAsync();
            ViewBag.DueFilters = Filters.DueFilterValues;
            ViewBag.Statuses = await statusRepo.GetAllStatuesAsync();

            if (loginManager.IsSignedIn(User))
            {
                var userTaskQuery = taskRepository.GetAllUserTask(userManager.GetUserId(User));
                var query = taskRepository.GetAllFilteredTask(filters, userTaskQuery);

                //Execute query and order by DueDate
                //Consider having mutiple order options
                var currentTask = await taskRepository.OrderTaskByDueDate(query);

                List<Task> acceptableTasks = OptimizerHelper(time, (List<Task>)currentTask);

                //return the optimized tasks to optimize view

                return View(acceptableTasks);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
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

        public List<Task> Helper(int maxtime, List<Task> tasks, Dictionary<string, List<Task>> memo , int index = 0, string s = "")
        {
            //base case
            if (index == 0 || maxtime == 0)
            {
                return [];
            }
            //Memoization
            if (memo.ContainsKey(s))
            {
                return memo[s];
            }
            //skip condition
            if (tasks[index - 1].Time > maxtime)
            {
                return Helper(maxtime, tasks, memo, index - 1);
            }
            
            //recursive calls
            List<Task> excludeTasks = Helper(maxtime, tasks, memo, index - 1, s);
            List<Task> includeTasks = Helper(maxtime - tasks[index - 1].Time, tasks, memo, index - 1, s);
            //add Task to the lists
            includeTasks.Add(tasks[index - 1]);
            
            //sum up all of the values within both lists
            int excludeTotal = excludeTasks.Sum(item => item.Value);
            int includeTotal = includeTasks.Sum(item => item.Value);
            //choose which list choices results in the highest total value
            if(includeTotal > excludeTotal)
            {
                s += tasks[index - 1].Id;
                memo[s] = includeTasks;
            }
            else
            {
                memo[s] = excludeTasks;
            }
            return memo[s];
        }
        //ToDo: Implement 0/1 Knapsack helper function 
        public List<Task> OptimizerHelper(int maxtime, List<Task> tasks)
        {
            //Initialize DP list
            List<List<int>> dp = new List<List<int>>(tasks.Count+1);
            for(int i = 1;i< tasks.Count + 1; i++)
            {
                dp.Add(new List<int>(maxtime + 1));
            }

            for(int i = 1; i<= tasks.Count; i++)
            {
                int value = tasks[i].Value;
                int cost = tasks[i].Time;
                for(int j = 1; j<= maxtime; j++)
                {
                    dp[i][j] = dp[i - 1][j];
                    if(j >= cost && dp[i - 1][j-cost] + value > dp[i][j])
                    {
                        dp[i][j] = dp[i - 1][j - cost] + value;
                    }
                }

            }

            //Find the task that where chosen 
            List<Task> selectedTasks = new List<Task>();
            int limit = maxtime;
            for(int i = tasks.Count; i>0; i--)
            {
                if (dp[i][limit] != dp[i - 1][limit])
                {
                    
                    selectedTasks.Add(tasks[i-1]);
                    limit -= tasks[i - 1].Time;
                }
            }
            
            return selectedTasks;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        
    }
}


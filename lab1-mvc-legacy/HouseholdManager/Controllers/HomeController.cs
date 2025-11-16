using HouseholdManager.Models;
using HouseholdManager.Models.Entities;
using HouseholdManager.Models.ViewModels;
using HouseholdManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace HouseholdManager.Controllers
{
    /// <summary>
    /// Handles public landing pages and static content. No authorization required.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly IUserService _userService;
        private readonly IHouseholdService _householdService;
        private readonly IHouseholdTaskService _taskService;
        private readonly ITaskExecutionService _executionService;
        private readonly ILogger<HomeController> _logger;

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        /// <param name="userService"></param>
        /// <param name="householdService"></param>
        /// <param name="taskService"></param>
        /// <param name="executionService"></param>
        /// <param name="logger"></param>
        public HomeController(
            IUserService userService,
            IHouseholdService householdService,
            IHouseholdTaskService taskService,
            ITaskExecutionService executionService,
            ILogger<HomeController> logger)
        {
            _userService = userService;
            _householdService = householdService;
            _taskService = taskService;
            _executionService = executionService;
            _logger = logger;
        }

        /// <summary>
        /// GET: Home/Index - Public landing page. Redirects authenticated users to Household/Index.
        /// </summary>
        /// <returns>View or redirect to Household/Index if authenticated</returns>
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Household");
            }

            return View();
        }

        /// <summary>
        /// GET: Home/About - Static about page
        /// </summary>
        /// <returns>About view</returns>
        public IActionResult About()
        {
            return View();
        }

        /// <summary>
        /// GET: Home/Privacy - Privacy policy page
        /// </summary>
        /// <returns>Privacy view</returns>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// GET: Home/Contact - Contact information page
        /// </summary>
        /// <returns>Contact view</returns>
        public IActionResult Contact()
        {
            return View();
        }

        /// <summary>
        /// GET: Home/Error - Global error handler with no caching
        /// </summary>
        /// <returns>Error view</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
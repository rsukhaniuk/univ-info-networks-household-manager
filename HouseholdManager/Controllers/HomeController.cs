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
    public class HomeController : Controller
    {
        private readonly IUserService _userService;
        private readonly IHouseholdService _householdService;
        private readonly IHouseholdTaskService _taskService;
        private readonly ITaskExecutionService _executionService;
        private readonly ILogger<HomeController> _logger;

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

        // Public landing page
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Household");
            }

            return View();
        }

        // Static pages
        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        // Error handling
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
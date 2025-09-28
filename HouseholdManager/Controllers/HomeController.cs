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
                return RedirectToAction("Dashboard");
            }

            return View();
        }

        // User dashboard (requires authentication)
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                // Get user's current household
                var currentHouseholdId = await _userService.GetCurrentHouseholdIdAsync(userId);

                var model = new DashboardViewModel
                {
                    UserStats = await _userService.GetUserDashboardStatsAsync(userId),
                    UserHouseholds = await _householdService.GetUserHouseholdsAsync(userId)
                };

                if (currentHouseholdId.HasValue)
                {
                    model.CurrentHousehold = await _householdService.GetHouseholdWithMembersAsync(currentHouseholdId.Value);
                    model.UserTasks = await _taskService.GetUserTasksAsync(userId);
                    model.RecentExecutions = await _executionService.GetUserExecutionsAsync(userId, currentHouseholdId.Value);
                    model.OverdueTasks = await _taskService.GetOverdueTasksAsync(currentHouseholdId.Value);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                TempData["Error"] = "An error occurred while loading your dashboard.";
                return View(new DashboardViewModel());
            }
        }

        // Switch current household
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SwitchHousehold(Guid? householdId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                if (householdId.HasValue)
                {
                    // Validate user is member of this household
                    var isMember = await _householdService.IsUserMemberAsync(householdId.Value, userId);
                    if (!isMember)
                    {
                        TempData["Error"] = "You are not a member of that household.";
                        return RedirectToAction("Dashboard");
                    }
                }

                await _userService.SetCurrentHouseholdAsync(userId, householdId);
                TempData["Success"] = householdId.HasValue
                    ? "Current household switched successfully."
                    : "Household selection cleared.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching household for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                TempData["Error"] = "An error occurred while switching households.";
            }

            return RedirectToAction("Dashboard");
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

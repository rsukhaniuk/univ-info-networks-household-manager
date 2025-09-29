using HouseholdManager.Models.Entities;
using HouseholdManager.Models.ViewModels;
using HouseholdManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HouseholdManager.Controllers
{
    [Authorize]
    public class ExecutionController : Controller
    {
        private readonly ITaskExecutionService _executionService;
        private readonly IHouseholdTaskService _taskService;
        private readonly ILogger<ExecutionController> _logger;

        public ExecutionController(
            ITaskExecutionService executionService,
            IHouseholdTaskService taskService,
            ILogger<ExecutionController> logger)
        {
            _executionService = executionService;
            _taskService = taskService;
            _logger = logger;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // POST: Execution/Complete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(Guid taskId, string? notes, IFormFile? photo)
        {
            try
            {
                var execution = await _executionService.CompleteTaskAsync(taskId, UserId, notes, photo);
                TempData["Success"] = "Task completed successfully!";

                return RedirectToAction("Details", "Task", new { id = taskId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", "Task", new { id = taskId });
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", "Task", new { id = taskId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing task {TaskId} by user {UserId}", taskId, UserId);
                TempData["Error"] = "An error occurred while completing the task.";
                return RedirectToAction("Details", "Task", new { id = taskId });
            }
        }

        // GET: Execution/History - Full execution history for a task
        public async Task<IActionResult> History(Guid taskId, int page = 1, int pageSize = 20)
        {
            try
            {
                await _taskService.ValidateTaskAccessAsync(taskId, UserId);

                var task = await _taskService.GetTaskWithRelationsAsync(taskId);
                if (task == null)
                    return NotFound();

                var allExecutions = await _executionService.GetTaskExecutionsAsync(taskId);
                var totalCount = allExecutions.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var executions = allExecutions
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new ExecutionHistoryViewModel
                {
                    Task = task,
                    Executions = executions,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalCount = totalCount,
                    PageSize = pageSize
                };

                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "You don't have access to this task.";
                return RedirectToAction("Index", "Household");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading execution history for task {TaskId}", taskId);
                TempData["Error"] = "An error occurred while loading execution history.";
                return RedirectToAction("Details", "Task", new { id = taskId });
            }
        }

        // GET: Execution/Details
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var execution = await _executionService.GetExecutionWithRelationsAsync(id);
                if (execution == null)
                    return NotFound();

                await _executionService.ValidateExecutionAccessAsync(id, UserId);

                return View(execution);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "You don't have access to this execution.";
                return RedirectToAction("Index", "Household");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading execution details {ExecutionId}", id);
                TempData["Error"] = "An error occurred while loading execution details.";
                return RedirectToAction("Index", "Household");
            }
        }

        // POST: Execution/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var execution = await _executionService.GetExecutionAsync(id);
                if (execution == null)
                    return NotFound();

                var taskId = execution.TaskId;

                await _executionService.DeleteExecutionAsync(id, UserId);

                TempData["Success"] = "Execution deleted successfully.";
                return RedirectToAction("Details", "Task", new { id = taskId });
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "You can only delete your own executions or be a household owner.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting execution {ExecutionId}", id);
                TempData["Error"] = "An error occurred while deleting the execution.";
                return RedirectToAction("Details", new { id });
            }
        }
    }
}

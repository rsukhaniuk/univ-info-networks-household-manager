using HouseholdManager.Models.Entities;
using HouseholdManager.Models.ViewModels;
using HouseholdManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HouseholdManager.Controllers
{
    /// <summary>
    /// Handles task execution (completion) and execution history. Any member can complete, only creator or Owner can delete.
    /// </summary>
    [Authorize]
    public class ExecutionController : Controller
    {
        private readonly ITaskExecutionService _executionService;
        private readonly IHouseholdTaskService _taskService;
        private readonly ILogger<ExecutionController> _logger;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="executionService"></param>
        /// <param name="taskService"></param>
        /// <param name="logger"></param>
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

        /// <summary>
        /// POST: Execution/Complete - Complete task with optional notes and photo. Regular tasks can only be completed once per week.
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="notes">Optional completion notes</param>
        /// <param name="photo">Optional completion photo</param>
        /// <returns>Redirect to Task/Details</returns>
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

        /// <summary>
        /// GET: Execution/History - Paginated execution history for task
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="page">Current page (default 1)</param>
        /// <param name="pageSize">Items per page (default 20)</param>
        /// <returns>View with ExecutionHistoryViewModel</returns>
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

        /// <summary>
        /// GET: Execution/Details - View single execution with task info, notes, photo, and user
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <returns>View with TaskExecution entity</returns>
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

        /// <summary>
        /// POST: Execution/Delete - Delete execution. Only creator or household Owner can delete. Deletes photo from filesystem.
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <returns>Redirect to Task/Details</returns>
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

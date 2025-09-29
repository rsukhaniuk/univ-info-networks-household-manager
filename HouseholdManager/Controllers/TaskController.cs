using HouseholdManager.Models.Entities;
using HouseholdManager.Models.Enums;
using HouseholdManager.Models.ViewModels;
using HouseholdManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace HouseholdManager.Controllers
{
    /// <summary>
    /// CRUD and management for household tasks. Owner for management, Member for viewing and completing assigned tasks.
    /// </summary>
    [Authorize]
    public class TaskController : Controller
    {
        private readonly IHouseholdTaskService _taskService;
        private readonly IHouseholdService _householdService;
        private readonly IRoomService _roomService;
        private readonly IHouseholdMemberService _memberService;
        private readonly ITaskExecutionService _executionService;
        private readonly ITaskAssignmentService _assignmentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TaskController> _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="taskService"></param>
        /// <param name="householdService"></param>
        /// <param name="roomService"></param>
        /// <param name="memberService"></param>
        /// <param name="executionService"></param>
        /// <param name="assignmentService"></param>
        /// <param name="userManager"></param>
        /// <param name="logger"></param>
        public TaskController(
            IHouseholdTaskService taskService,
            IHouseholdService householdService,
            IRoomService roomService,
            IHouseholdMemberService memberService,
            ITaskExecutionService executionService,
            ITaskAssignmentService assignmentService,
            UserManager<ApplicationUser> userManager,
            ILogger<TaskController> logger)
        {
            _taskService = taskService;
            _householdService = householdService;
            _roomService = roomService;
            _memberService = memberService;
            _executionService = executionService;
            _assignmentService = assignmentService;
            _userManager = userManager;
            _logger = logger;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        /// <summary>
        /// GET: Task/Index - List and filter tasks by room, priority, assignee, status, or search query
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="roomId">Optional room filter</param>
        /// <param name="priority">Optional priority filter</param>
        /// <param name="assigneeId">Optional assignee filter ("unassigned" for unassigned tasks)</param>
        /// <param name="search">Optional search in title/description</param>
        /// <param name="status">Optional status filter ("active"/"inactive")</param>
        /// <returns>View with TaskIndexViewModel and filter dropdowns</returns>
        public async Task<IActionResult> Index(Guid householdId, Guid? roomId = null,
            TaskPriority? priority = null, string? assigneeId = null, string? search = null, string? status = null)
        {
            try
            {
                await _householdService.ValidateUserAccessAsync(householdId, UserId);

                var household = await _householdService.GetHouseholdAsync(householdId);
                if (household == null)
                    return NotFound();

                var tasks = await _taskService.GetHouseholdTasksAsync(householdId);

                // Apply filters
                if (roomId.HasValue)
                    tasks = tasks.Where(t => t.RoomId == roomId.Value).ToList();

                if (priority.HasValue)
                    tasks = tasks.Where(t => t.Priority == priority.Value).ToList();

                if (!string.IsNullOrEmpty(assigneeId))
                {
                    if (assigneeId == "unassigned")
                        tasks = tasks.Where(t => string.IsNullOrEmpty(t.AssignedUserId)).ToList();
                    else
                        tasks = tasks.Where(t => t.AssignedUserId == assigneeId).ToList();
                }

                if (!string.IsNullOrEmpty(status))
                {
                    if (status == "active")
                        tasks = tasks.Where(t => t.IsActive).ToList();
                    else if (status == "inactive")
                        tasks = tasks.Where(t => !t.IsActive).ToList();
                }

                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    tasks = tasks.Where(t =>
                        t.Title.ToLower().Contains(searchLower) ||
                        (t.Description != null && t.Description.ToLower().Contains(searchLower))
                    ).ToList();
                }

                var isOwner = await _householdService.IsUserOwnerAsync(householdId, UserId);
                var rooms = await _roomService.GetHouseholdRoomsAsync(householdId);
                var members = await _memberService.GetHouseholdMembersAsync(householdId);

                var model = new TaskIndexViewModel
                {
                    Household = household,
                    Tasks = tasks,
                    IsOwner = isOwner,
                    Rooms = rooms.Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name }).ToList(),
                    Members = members.Select(m => new SelectListItem
                    {
                        Value = m.UserId,
                        Text = m.User.FullName ?? m.User.Email ?? m.UserId
                    }).ToList(),
                    SelectedRoomId = roomId,
                    SelectedStatus = status,
                    SelectedPriority = priority,
                    SelectedAssigneeId = assigneeId,
                    SearchQuery = search
                };

                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "You don't have access to this household.";
                return RedirectToAction("Index", "Household");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tasks for household {HouseholdId}", householdId);
                TempData["Error"] = "An error occurred while loading tasks.";
                return RedirectToAction("Details", "Household", new { id = householdId });
            }
        }

        /// <summary>
        /// GET: Task/Details - Task details with inline completion form and last 5 executions
        /// </summary>
        /// <param name="id">Task ID</param>
        /// <returns>View with TaskDetailsViewModel including canComplete flag</returns>
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                await _taskService.ValidateTaskAccessAsync(id, UserId);

                var task = await _taskService.GetTaskWithRelationsAsync(id);
                if (task == null)
                    return NotFound();

                var currentUser = await _userManager.GetUserAsync(User);
                var isSystemAdmin = currentUser?.IsSystemAdmin ?? false;
                var isOwner = await _householdService.IsUserOwnerAsync(task.HouseholdId, UserId);
                var isAssignee = task.AssignedUserId == UserId;
                var executions = await _executionService.GetTaskExecutionsAsync(id);
                var isCompletedThisWeek = task.Type == TaskType.Regular
                    ? await _executionService.IsTaskCompletedThisWeekAsync(id)
                    : false;
                var householdMembers = await _memberService.GetHouseholdMembersAsync(task.HouseholdId);

                // Can complete if: SystemAdmin OR Owner OR (Member AND Assignee)
                var canComplete = task.IsActive && (isSystemAdmin || isOwner || isAssignee);

                var model = new TaskDetailsViewModel
                {
                    Task = task,
                    Executions = executions.Take(5).ToList(), // Show last 5 executions
                    IsOwner = isOwner,
                    IsSystemAdmin = isSystemAdmin,
                    IsAssignedToCurrentUser = isAssignee,
                    IsCompletedThisWeek = isCompletedThisWeek,
                    CanComplete = canComplete,
                    HouseholdMembers = householdMembers
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
                _logger.LogError(ex, "Error loading task details {TaskId}", id);
                TempData["Error"] = "An error occurred while loading task details.";
                return RedirectToAction("Index", "Household");
            }
        }

        /// <summary>
        /// GET: Task/Upsert - Create or edit task form with room and member dropdowns
        /// </summary>
        /// <param name="id">Task ID for edit mode, null for create mode</param>
        /// <param name="householdId">Required for create mode</param>
        /// <returns>View with TaskUpsertViewModel</returns>
        public async Task<IActionResult> Upsert(Guid? id, Guid? householdId)
        {
            try
            {
                TaskUpsertViewModel model;

                if (id.HasValue)
                {
                    // Edit mode
                    await _taskService.ValidateTaskOwnerAccessAsync(id.Value, UserId);

                    var task = await _taskService.GetTaskWithRelationsAsync(id.Value);
                    if (task == null)
                        return NotFound();

                    model = new TaskUpsertViewModel
                    {
                        Id = task.Id,
                        HouseholdId = task.HouseholdId,
                        Title = task.Title,
                        Description = task.Description,
                        Type = task.Type,
                        Priority = task.Priority,
                        EstimatedMinutes = task.EstimatedMinutes,
                        RoomId = task.RoomId,
                        AssignedUserId = task.AssignedUserId,
                        IsActive = task.IsActive,
                        DueDate = task.DueDate,
                        ScheduledWeekday = task.ScheduledWeekday,
                        RowVersion = task.RowVersion,
                        IsEdit = true
                    };

                    await PopulateUpsertViewModel(model, task.HouseholdId);
                }
                else
                {
                    // Create mode
                    if (!householdId.HasValue)
                        return BadRequest("Household ID is required for creating a task.");

                    await _householdService.ValidateOwnerAccessAsync(householdId.Value, UserId);

                    model = new TaskUpsertViewModel
                    {
                        HouseholdId = householdId.Value,
                        Priority = TaskPriority.Medium,
                        EstimatedMinutes = 30,
                        IsActive = true,
                        IsEdit = false
                    };

                    await PopulateUpsertViewModel(model, householdId.Value);
                }

                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can create or edit tasks.";
                return RedirectToAction("Index", "Household");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading task upsert form");
                TempData["Error"] = "An error occurred while loading the form.";
                return RedirectToAction("Index", "Household");
            }
        }

        /// <summary>
        /// POST: Task/Upsert - Save task with custom validation (OneTime requires DueDate, Regular requires ScheduledWeekday)
        /// </summary>
        /// <param name="model">Task data to save</param>
        /// <returns>Redirect to Details on success, View with errors on failure</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(TaskUpsertViewModel model)
        {
            // Custom validation for type-specific fields
            if (model.Type == TaskType.OneTime)
            {
                if (!model.DueDate.HasValue)
                    ModelState.AddModelError(nameof(model.DueDate), "Due date is required for one-time tasks.");

                model.ScheduledWeekday = null; // Clear weekday for one-time tasks
            }
            else if (model.Type == TaskType.Regular)
            {
                if (!model.ScheduledWeekday.HasValue)
                    ModelState.AddModelError(nameof(model.ScheduledWeekday), "Scheduled weekday is required for regular tasks.");

                model.DueDate = null; // Clear due date for regular tasks
            }

            if (!ModelState.IsValid)
            {
                await PopulateUpsertViewModel(model, model.HouseholdId);
                return View(model);
            }

            try
            {
                if (model.IsEdit)
                {
                    // Update existing task
                    await _taskService.ValidateTaskOwnerAccessAsync(model.Id, UserId);

                    var task = await _taskService.GetTaskAsync(model.Id);
                    if (task == null)
                        return NotFound();

                    // Update properties
                    task.Title = model.Title!;
                    task.Description = model.Description;
                    task.Type = model.Type;
                    task.Priority = model.Priority;
                    task.EstimatedMinutes = model.EstimatedMinutes;
                    task.RoomId = model.RoomId;
                    task.AssignedUserId = string.IsNullOrEmpty(model.AssignedUserId) ? null : model.AssignedUserId;
                    task.IsActive = model.IsActive;
                    task.DueDate = model.DueDate;
                    task.ScheduledWeekday = model.ScheduledWeekday;

                    await _taskService.UpdateTaskAsync(task, UserId);

                    TempData["Success"] = "Task updated successfully!";
                    return RedirectToAction("Details", new { id = task.Id });
                }
                else
                {
                    // Create new task
                    await _householdService.ValidateOwnerAccessAsync(model.HouseholdId, UserId);

                    var task = new HouseholdTask
                    {
                        Title = model.Title!,
                        Description = model.Description,
                        Type = model.Type,
                        Priority = model.Priority,
                        EstimatedMinutes = model.EstimatedMinutes,
                        HouseholdId = model.HouseholdId,
                        RoomId = model.RoomId,
                        AssignedUserId = string.IsNullOrEmpty(model.AssignedUserId) ? null : model.AssignedUserId,
                        IsActive = model.IsActive,
                        DueDate = model.DueDate,
                        ScheduledWeekday = model.ScheduledWeekday
                    };

                    var createdTask = await _taskService.CreateTaskAsync(task, UserId);

                    TempData["Success"] = "Task created successfully!";
                    return RedirectToAction("Details", new { id = createdTask.Id });
                }
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can create or edit tasks.";
                return RedirectToAction("Index", new { householdId = model.HouseholdId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                await PopulateUpsertViewModel(model, model.HouseholdId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting task");
                ModelState.AddModelError("", "An error occurred while saving the task.");
                await PopulateUpsertViewModel(model, model.HouseholdId);
                return View(model);
            }
        }

        /// <summary>
        /// POST: Task/Delete - Delete task permanently. SystemAdmin can always delete, otherwise must be Owner.
        /// </summary>
        /// <param name="id">Task ID</param>
        /// <returns>Redirect to Task/Index for household</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var task = await _taskService.GetTaskAsync(id);
                if (task == null)
                    return NotFound();

                var currentUser = await _userManager.GetUserAsync(User);
                var isSystemAdmin = currentUser?.IsSystemAdmin ?? false;

                // SystemAdmin can always delete, otherwise must be owner
                if (!isSystemAdmin)
                {
                    await _taskService.ValidateTaskOwnerAccessAsync(id, UserId);
                }

                await _taskService.DeleteTaskAsync(id, UserId);

                TempData["Success"] = "Task deleted successfully.";
                return RedirectToAction("Index", new { householdId = task.HouseholdId });
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can delete tasks.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task {TaskId}", id);
                TempData["Error"] = "An error occurred while deleting the task.";
                return RedirectToAction("Details", new { id });
            }
        }

        /// <summary>
        /// POST: Task/ReassignNext - Rotate task to next member using round-robin algorithm. Owner only.
        /// Not currently used.
        /// </summary>
        /// <param name="id">Task ID</param>
        /// <returns>Redirect to Details</returns>
        /// <remarks>
        /// TODO: Requires additional testing and validation
        /// </remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReassignNext(Guid id)
        {
            try
            {
                await _taskService.ValidateTaskOwnerAccessAsync(id, UserId);

                var newAssigneeId = await _taskService.ReassignTaskToNextUserAsync(id, UserId);

                TempData["Success"] = "Task reassigned to next user successfully!";
                return RedirectToAction("Details", new { id });
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can reassign tasks.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning task {TaskId}", id);
                TempData["Error"] = "An error occurred while reassigning the task.";
                return RedirectToAction("Details", new { id });
            }
        }

        /// <summary>
        /// POST: Task/AutoAssign - Auto-assign all unassigned tasks using fair distribution algorithm. Owner only.
        /// Not currently used.
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <returns>Redirect to Task/Index with assignment count</returns>
        /// <remarks>
        /// TODO: Requires additional testing and validation
        /// </remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoAssign(Guid householdId)
        {
            try
            {
                await _householdService.ValidateOwnerAccessAsync(householdId, UserId);

                await _taskService.AutoAssignTasksAsync(householdId, UserId);

                TempData["Success"] = "All unassigned tasks have been automatically assigned!";
                return RedirectToAction("Index", new { householdId });
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can auto-assign tasks.";
                return RedirectToAction("Index", new { householdId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-assigning tasks for household {HouseholdId}", householdId);
                TempData["Error"] = "An error occurred while auto-assigning tasks.";
                return RedirectToAction("Index", new { householdId });
            }
        }

        /// <summary>
        /// Helper method to populate room and member dropdown lists for Upsert view
        /// </summary>
        /// <param name="model">ViewModel to populate</param>
        /// <param name="householdId">Household ID</param>
        /// <returns>Task</returns>
        private async Task PopulateUpsertViewModel(TaskUpsertViewModel model, Guid householdId)
        {
            var household = await _householdService.GetHouseholdAsync(householdId);
            var rooms = await _roomService.GetHouseholdRoomsAsync(householdId);
            var members = await _memberService.GetHouseholdMembersAsync(householdId);

            model.HouseholdName = household?.Name ?? "Unknown";
            model.Rooms = rooms.Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name }).ToList();
            model.Members = members.Select(m => new SelectListItem
            {
                Value = m.UserId,
                Text = m.User.FullName ?? m.User.Email ?? m.UserId
            }).ToList();
        }

        /// <summary>
        /// POST: Task/Activate - Activate task (sets IsActive=true). Owner only.
        /// </summary>
        /// <param name="id">Task ID</param>
        /// <returns>Redirect to Details</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(Guid id)
        {
            try
            {
                await _taskService.ActivateTaskAsync(id, UserId);
                TempData["Success"] = "Task activated successfully!";
                return RedirectToAction("Details", new { id });
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can activate tasks.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating task {TaskId}", id);
                TempData["Error"] = "An error occurred while activating the task.";
                return RedirectToAction("Details", new { id });
            }
        }

        /// <summary>
        /// POST: Task/Deactivate - Deactivate task (sets IsActive=false). Owner only.
        /// </summary>
        /// <param name="id">Task ID</param>
        /// <returns>Redirect to Details</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            try
            {
                await _taskService.DeactivateTaskAsync(id, UserId);
                TempData["Success"] = "Task deactivated successfully!";
                return RedirectToAction("Details", new { id });
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can deactivate tasks.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating task {TaskId}", id);
                TempData["Error"] = "An error occurred while deactivating the task.";
                return RedirectToAction("Details", new { id });
            }
        }
    }
}

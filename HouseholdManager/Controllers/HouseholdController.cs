using HouseholdManager.Models.Entities;
using HouseholdManager.Models.ViewModels;
using HouseholdManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HouseholdManager.Controllers
{
    /// <summary>
    /// CRUD operations for households and membership management. Owner for management, Member for viewing.
    /// </summary>
    [Authorize]
    public class HouseholdController : Controller
    {
        private readonly IHouseholdService _householdService;
        private readonly IHouseholdMemberService _memberService;
        private readonly IRoomService _roomService;
        private readonly IHouseholdTaskService _taskService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HouseholdController> _logger;

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        /// <param name="householdService"></param>
        /// <param name="memberService"></param>
        /// <param name="roomService"></param>
        /// <param name="taskService"></param>
        /// <param name="userManager"></param>
        /// <param name="logger"></param>
        public HouseholdController(
            IHouseholdService householdService,
            IHouseholdMemberService memberService,
            IRoomService roomService,
            IHouseholdTaskService taskService,
            UserManager<ApplicationUser> userManager,
            ILogger<HouseholdController> logger)
        {
            _householdService = householdService;
            _memberService = memberService;
            _roomService = roomService;
            _taskService = taskService;
            _userManager = userManager;
            _logger = logger;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        /// <summary>
        /// GET: Household/Index - List user's households. SystemAdmin sees all, regular users see only their memberships.
        /// </summary>
        /// <returns>View with list of households</returns>
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                IReadOnlyList<Household> households;

                if (user?.IsSystemAdmin == true)
                {
                    households = await _householdService.GetAllHouseholdsAsync();
                }
                else
                {
                    households = await _householdService.GetUserHouseholdsAsync(UserId);
                }

                return View(households);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading households for user {UserId}", UserId);
                TempData["Error"] = "An error occurred while loading your households.";
                return View(new List<Household>());
            }
        }

        /// <summary>
        /// GET: Household/Details - Household overview with members, rooms, and active tasks
        /// </summary>
        /// <param name="id">Household ID</param>
        /// <returns>View with HouseholdDetailsViewModel</returns>
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                await _householdService.ValidateUserAccessAsync(id, UserId);

                var household = await _householdService.GetHouseholdWithMembersAsync(id);
                if (household == null)
                    return NotFound();

                var model = new HouseholdDetailsViewModel
                {
                    Household = household,
                    Rooms = await _roomService.GetHouseholdRoomsAsync(id),
                    ActiveTasks = await _taskService.GetActiveHouseholdTasksAsync(id),
                    IsOwner = await _householdService.IsUserOwnerAsync(id, UserId)
                };

                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "You don't have access to this household.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading household details {HouseholdId}", id);
                TempData["Error"] = "An error occurred while loading household details.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// GET: Household/Upsert - Create or edit household form (Upsert pattern)
        /// </summary>
        /// <param name="id">Household ID for edit mode, null for create mode</param>
        /// <returns>View with UpsertHouseholdViewModel</returns>
        public async Task<IActionResult> Upsert(Guid? id)
        {
            try
            {
                if (id.HasValue)
                {
                    // Edit mode
                    await _householdService.ValidateOwnerAccessAsync(id.Value, UserId);

                    var household = await _householdService.GetHouseholdAsync(id.Value);
                    if (household == null)
                        return NotFound();

                    var model = new UpsertHouseholdViewModel
                    {
                        Id = household.Id,
                        Name = household.Name,
                        Description = household.Description,
                        IsEdit = true
                    };
                    return View(model);
                }
                else
                {
                    // Create mode
                    return View(new UpsertHouseholdViewModel { IsEdit = false });
                }
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "You don't have permission to edit this household.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// POST: Household/Upsert - Save household. Create auto-adds creator as Owner, edit updates Name/Description.
        /// </summary>
        /// <param name="model">Household data to save</param>
        /// <returns>Redirect to Details on success, View with errors on failure</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(UpsertHouseholdViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                if (model.IsEdit)
                {
                    // Update existing
                    await _householdService.ValidateOwnerAccessAsync(model.Id, UserId);
                    var household = await _householdService.GetHouseholdAsync(model.Id);
                    if (household == null)
                        return NotFound();

                    household.Name = model.Name!;
                    household.Description = model.Description;
                    await _householdService.UpdateHouseholdAsync(household);

                    TempData["Success"] = "Household updated successfully!";
                }
                else
                {
                    // Create new
                    var household = await _householdService.CreateHouseholdAsync(
                        model.Name!, model.Description, UserId);

                    TempData["Success"] = "Household created successfully!";
                    model.Id = household.Id;
                }

                return RedirectToAction("Details", new { id = model.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Upsert for household {HouseholdId}", model.Id);
                ModelState.AddModelError("", "An error occurred while saving the household.");
                return View(model);
            }
        }


        /// <summary>
        /// GET: Household/Join - Display join household form
        /// </summary>
        /// <returns>View with JoinHouseholdViewModel</returns>
        public IActionResult Join()
        {
            return View(new JoinHouseholdViewModel());
        }

        /// <summary>
        /// POST: Household/Join - Join household using Guid invite code
        /// </summary>
        /// <param name="model">Model containing invite code</param>
        /// <returns>Redirect to Details on success, View with errors on failure</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(JoinHouseholdViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                if (!model.InviteCode.HasValue)
                {
                    ModelState.AddModelError("InviteCode", "Please enter an invite code.");
                    return View(model);
                }
                var member = await _householdService.JoinHouseholdAsync(model.InviteCode.Value, UserId);
                TempData["Success"] = "Successfully joined the household!";
                return RedirectToAction("Details", new { id = member.HouseholdId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("InviteCode", ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining household with invite code {InviteCode}", model.InviteCode);
                ModelState.AddModelError("", "An error occurred while joining the household.");
                return View(model);
            }
        }

        /// <summary>
        /// POST: Household/Leave - Leave household. Cannot leave if last Owner.
        /// </summary>
        /// <param name="id">Household ID</param>
        /// <returns>Redirect to Index on success, Details with error on failure</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(Guid id)
        {
            try
            {
                await _householdService.LeaveHouseholdAsync(id, UserId);
                TempData["Success"] = "You have left the household.";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving household {HouseholdId}", id);
                TempData["Error"] = "An error occurred while leaving the household.";
                return RedirectToAction("Details", new { id });
            }
        }

        /// <summary>
        /// POST: Household/RemoveMember - Owner removes member from household. Cannot remove last Owner.
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="userId">User ID to remove</param>
        /// <returns>Redirect to Details</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(Guid householdId, string userId)
        {
            try
            {
                await _householdService.RemoveMemberAsync(householdId, userId, UserId);
                TempData["Success"] = "Member removed successfully.";
                return RedirectToAction("Details", new { id = householdId });
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can remove members.";
                return RedirectToAction("Details", new { id = householdId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", new { id = householdId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member from household {HouseholdId}", householdId);
                TempData["Error"] = "An error occurred while removing the member.";
                return RedirectToAction("Details", new { id = householdId });
            }
        }

        /// <summary>
        /// POST: Household/RegenerateInviteCode - Generate new unique invite code. Owner only.
        /// </summary>
        /// <param name="id">Household ID</param>
        /// <returns>Redirect to Details with new code</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateInviteCode(Guid id)
        {
            try
            {
                var newCode = await _householdService.RegenerateInviteCodeAsync(id, UserId);
                TempData["Success"] = "Invite code regenerated successfully!";
                return RedirectToAction("Details", new { id }); // Changed from Edit to Details
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can regenerate invite codes.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating invite code for household {HouseholdId}", id);
                TempData["Error"] = "An error occurred while regenerating the invite code.";
                return RedirectToAction("Details", new { id }); // Changed from Edit to Details
            }
        }

        /// <summary>
        /// GET: Household/Delete - Delete confirmation page. Owner only.
        /// </summary>
        /// <param name="id">Household ID</param>
        /// <returns>View with household details for confirmation</returns>
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _householdService.ValidateOwnerAccessAsync(id, UserId);

                var household = await _householdService.GetHouseholdWithMembersAsync(id);
                if (household == null)
                    return NotFound();

                return View(household);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can delete households.";
                return RedirectToAction("Details", new { id });
            }
        }

        /// <summary>
        /// POST: Household/Delete - Delete household permanently. Cascades to rooms, tasks, executions, memberships.
        /// </summary>
        /// <param name="id">Household ID</param>
        /// <returns>Redirect to Index on success</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                await _householdService.DeleteHouseholdAsync(id, UserId);
                TempData["Success"] = "Household deleted successfully.";
                return RedirectToAction("Index");
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can delete households.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting household {HouseholdId}", id);
                TempData["Error"] = "An error occurred while deleting the household.";
                return RedirectToAction("Details", new { id });
            }
        }
    }
}

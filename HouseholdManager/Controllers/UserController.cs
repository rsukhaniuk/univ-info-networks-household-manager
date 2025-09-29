using HouseholdManager.Models.Entities;
using HouseholdManager.Models.Enums;
using HouseholdManager.Models.ViewModels;
using HouseholdManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HouseholdManager.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IHouseholdMemberService _memberService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserService userService,
            IHouseholdMemberService memberService,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _memberService = memberService;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // GET: User/Profile
        public async Task<IActionResult> Profile()
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(UserId);
                if (user == null)
                    return NotFound();

                var stats = await _userService.GetUserDashboardStatsAsync(UserId);

                var model = new UserProfileViewModel
                {
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email!,
                    UserName = user.UserName,
                    CreatedAt = user.CreatedAt,
                    TotalHouseholds = stats.TotalHouseholds,
                    OwnedHouseholds = stats.OwnedHouseholds,
                    ActiveTasks = stats.ActiveTasks,
                    CompletedTasksThisWeek = stats.CompletedTasksThisWeek
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile for user {UserId}", UserId);
                TempData["Error"] = "An error occurred while loading your profile.";
                return RedirectToAction("Dashboard", "Home");
            }
        }

        // POST: User/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _userService.UpdateUserProfileAsync(
                    UserId,
                    model.FirstName,
                    model.LastName,
                    model.Email
                );

                if (result.Succeeded)
                {
                    // Refresh sign in to update claims
                    var user = await _userService.GetUserByIdAsync(UserId);
                    await _signInManager.RefreshSignInAsync(user!);

                    TempData["Success"] = "Profile updated successfully!";
                    return RedirectToAction("Profile");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", UserId);
                ModelState.AddModelError("", "An error occurred while updating your profile.");
                return View(model);
            }
        }

        // POST: User/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the errors and try again.";
                return RedirectToAction("Profile");
            }

            try
            {
                var result = await _userService.ChangePasswordAsync(
                    UserId,
                    model.CurrentPassword,
                    model.NewPassword
                );

                if (result.Succeeded)
                {
                    TempData["Success"] = "Password changed successfully!";
                }
                else
                {
                    TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
                }

                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", UserId);
                TempData["Error"] = "An error occurred while changing your password.";
                return RedirectToAction("Profile");
            }
        }

        // GET: User/DeleteAccount
        public IActionResult DeleteAccount()
        {
            return View();
        }

        // POST: User/DeleteAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccountConfirmed(string confirmPassword)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(UserId);
                if (user == null)
                    return NotFound();

                // Verify password before deletion
                var passwordValid = await _userManager.CheckPasswordAsync(user, confirmPassword);
                if (!passwordValid)
                {
                    TempData["Error"] = "Incorrect password. Account deletion cancelled.";
                    return RedirectToAction("DeleteAccount");
                }

                // Check if user is the last owner of any households
                var memberships = await _memberService.GetUserMembershipsAsync(UserId);
                var ownedHouseholds = memberships.Where(m => m.Role == HouseholdRole.Owner).ToList();

                //foreach (var membership in ownedHouseholds)
                //{
                //    var ownerCount = await _memberService.GetOwnerCountAsync(membership.HouseholdId);
                //    if (ownerCount <= 1)
                //    {
                //        TempData["Error"] = $"You are the last owner of household '{membership.Household.Name}'. Please transfer ownership or delete the household before deleting your account.";
                //        return RedirectToAction("DeleteAccount");
                //    }
                //}

                // Delete user account
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    await _signInManager.SignOutAsync();
                    _logger.LogInformation("User {UserId} deleted their account", UserId);
                    TempData["Success"] = "Your account has been permanently deleted.";
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                TempData["Error"] = "An error occurred while deleting your account.";
                return RedirectToAction("DeleteAccount");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account for user {UserId}", UserId);
                TempData["Error"] = "An error occurred while deleting your account.";
                return RedirectToAction("DeleteAccount");
            }
        }

        // GET: User/AdminPanel - System admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminPanel(string? search = null)
        {
            try
            {
                var users = string.IsNullOrEmpty(search)
                    ? await _userService.GetAllUsersAsync()
                    : await _userService.SearchUsersAsync(search);

                var model = new AdminPanelViewModel
                {
                    Users = users,
                    SearchQuery = search
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin panel");
                TempData["Error"] = "An error occurred while loading the admin panel.";
                return RedirectToAction("Dashboard", "Home");
            }
        }

        // POST: User/SetSystemRole - System admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> SetSystemRole(string userId, SystemRole role)
        {
            try
            {
                var result = await _userService.SetSystemRoleAsync(userId, role, UserId);

                if (result.Succeeded)
                {
                    TempData["Success"] = "User role updated successfully!";
                }
                else
                {
                    TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
                }

                return RedirectToAction("AdminPanel");
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("AdminPanel");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting system role for user {UserId}", userId);
                TempData["Error"] = "An error occurred while updating the user role.";
                return RedirectToAction("AdminPanel");
            }
        }

        // POST: User/DeleteUser - System admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(userId, UserId);

                if (result.Succeeded)
                {
                    TempData["Success"] = "User deleted successfully!";
                }
                else
                {
                    TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
                }

                return RedirectToAction("AdminPanel");
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("AdminPanel");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                TempData["Error"] = "An error occurred while deleting the user.";
                return RedirectToAction("AdminPanel");
            }
        }

        // POST: User/PromoteToOwner
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToOwner(Guid householdId, string userId)
        {
            try
            {
                await _memberService.PromoteToOwnerAsync(householdId, userId, UserId);
                TempData["Success"] = "Member promoted to owner successfully!";
                return RedirectToAction("ManageMembers", new { householdId });
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("ManageMembers", new { householdId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting user {UserId} in household {HouseholdId}", userId, householdId);
                TempData["Error"] = "An error occurred while promoting the member.";
                return RedirectToAction("ManageMembers", new { householdId });
            }
        }

        // POST: User/DemoteFromOwner
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DemoteFromOwner(Guid householdId, string userId)
        {
            try
            {
                await _memberService.DemoteFromOwnerAsync(householdId, userId, UserId);
                TempData["Success"] = "Owner demoted to member successfully!";
                return RedirectToAction("ManageMembers", new { householdId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("ManageMembers", new { householdId });
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("ManageMembers", new { householdId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error demoting user {UserId} in household {HouseholdId}", userId, householdId);
                TempData["Error"] = "An error occurred while demoting the owner.";
                return RedirectToAction("ManageMembers", new { householdId });
            }
        }

        // POST: User/RemoveMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(Guid householdId, string userId)
        {
            try
            {
                var household = await _memberService.GetHouseholdMembersAsync(householdId);
                await _memberService.ValidateOwnerAccessAsync(householdId, UserId);

                var member = await _memberService.GetMemberAsync(householdId, userId);
                if (member == null)
                {
                    TempData["Error"] = "Member not found.";
                    return RedirectToAction("ManageMembers", new { householdId });
                }

                // Check if removing last owner
                if (member.Role == HouseholdRole.Owner)
                {
                    var ownerCount = await _memberService.GetOwnerCountAsync(householdId);
                    if (ownerCount <= 1)
                    {
                        TempData["Error"] = "Cannot remove the last owner of the household.";
                        return RedirectToAction("ManageMembers", new { householdId });
                    }
                }

                // Remove member through HouseholdService
                var householdService = HttpContext.RequestServices.GetRequiredService<IHouseholdService>();
                await householdService.RemoveMemberAsync(householdId, userId, UserId);

                TempData["Success"] = "Member removed successfully!";
                return RedirectToAction("ManageMembers", new { householdId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("ManageMembers", new { householdId });
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("ManageMembers", new { householdId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {UserId} from household {HouseholdId}", userId, householdId);
                TempData["Error"] = "An error occurred while removing the member.";
                return RedirectToAction("ManageMembers", new { householdId });
            }
        }
    }
}

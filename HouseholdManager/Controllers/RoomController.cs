using HouseholdManager.Models.Entities;
using HouseholdManager.Models.ViewModels;
using HouseholdManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace HouseholdManager.Controllers
{
    /// <summary>
    /// CRUD operations for rooms within households. Owner only for all modifications. Supports photo upload.
    /// </summary>
    [Authorize]
    public class RoomController : Controller
    {
        private readonly IRoomService _roomService;
        private readonly IHouseholdService _householdService;
        private readonly IHouseholdTaskService _taskService;
        private readonly ILogger<RoomController> _logger;

        public RoomController(
            IRoomService roomService,
            IHouseholdService householdService,
            IHouseholdTaskService taskService,
            ILogger<RoomController> logger)
        {
            _roomService = roomService;
            _householdService = householdService;
            _taskService = taskService;
            _logger = logger;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        /// <summary>
        /// GET: Room/Index - List rooms in household with task counts
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <returns>View with RoomIndexViewModel</returns>
        public async Task<IActionResult> Index(Guid householdId)
        {
            try
            {
                await _householdService.ValidateUserAccessAsync(householdId, UserId);

                var household = await _householdService.GetHouseholdAsync(householdId);
                if (household == null)
                    return NotFound();

                var rooms = await _roomService.GetHouseholdRoomsAsync(householdId);
                var isOwner = await _householdService.IsUserOwnerAsync(householdId, UserId);

                var model = new RoomIndexViewModel
                {
                    Household = household,
                    Rooms = rooms,
                    IsOwner = isOwner
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
                _logger.LogError(ex, "Error loading rooms for household {HouseholdId}", householdId);
                TempData["Error"] = "An error occurred while loading rooms.";
                return RedirectToAction("Details", "Household", new { id = householdId });
            }
        }

        /// <summary>
        /// GET: Room/Details - Room details with associated tasks
        /// </summary>
        /// <param name="id">Room ID</param>
        /// <returns>View with RoomDetailsViewModel</returns>
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                await _roomService.ValidateRoomAccessAsync(id, UserId);

                var room = await _roomService.GetRoomWithTasksAsync(id);
                if (room == null)
                    return NotFound();

                // Load household separately since GetRoomWithTasksAsync doesn't include it
                var household = await _householdService.GetHouseholdAsync(room.HouseholdId);
                if (household == null)
                    return NotFound();

                // Set the household navigation property
                room.Household = household;

                var isOwner = await _householdService.IsUserOwnerAsync(room.HouseholdId, UserId);

                var model = new RoomDetailsViewModel
                {
                    Room = room,
                    IsOwner = isOwner
                };

                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "You don't have access to this room.";
                return RedirectToAction("Index", "Household");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading room details {RoomId}", id);
                TempData["Error"] = "An error occurred while loading room details.";
                return RedirectToAction("Index", "Household");
            }
        }

        /// <summary>
        /// GET: Room/Upsert - Create or edit room form (Upsert pattern)
        /// </summary>
        /// <param name="id">Room ID for edit mode, null for create mode</param>
        /// <param name="householdId">Required for create mode</param>
        /// <returns>View with UpsertRoomViewModel</returns>
        public async Task<IActionResult> Upsert(Guid? id, Guid? householdId)
        {
            try
            {
                if (id.HasValue)
                {
                    // Edit mode
                    await _roomService.ValidateRoomOwnerAccessAsync(id.Value, UserId);

                    var room = await _roomService.GetRoomAsync(id.Value);
                    if (room == null)
                        return NotFound();

                    var household = await _householdService.GetHouseholdAsync(room.HouseholdId);

                    var model = new UpsertRoomViewModel
                    {
                        Id = room.Id,
                        HouseholdId = room.HouseholdId,
                        HouseholdName = household?.Name ?? "",
                        Name = room.Name,
                        Description = room.Description,
                        Priority = room.Priority,
                        PhotoPath = room.PhotoPath,
                        IsEdit = true
                    };
                    return View(model);
                }
                else
                {
                    // Create mode
                    if (!householdId.HasValue)
                        return BadRequest("HouseholdId is required for creating a room");

                    await _householdService.ValidateOwnerAccessAsync(householdId.Value, UserId);

                    var household = await _householdService.GetHouseholdAsync(householdId.Value);
                    if (household == null)
                        return NotFound();

                    var model = new UpsertRoomViewModel
                    {
                        HouseholdId = householdId.Value,
                        HouseholdName = household.Name,
                        Priority = 5, // Default priority
                        IsEdit = false
                    };
                    return View(model);
                }
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can create or edit rooms.";
                if (householdId.HasValue)
                    return RedirectToAction("Details", "Household", new { id = householdId });
                else if (id.HasValue)
                    return RedirectToAction("Details", new { id });
                else
                    return RedirectToAction("Index", "Household");
            }
        }

        /// <summary>
        /// POST: Room/Upsert - Save room with optional photo. Validates name uniqueness within household.
        /// </summary>
        /// <param name="model">Room data to save</param>
        /// <param name="photo">Optional room photo file</param>
        /// <returns>Redirect to Details on success, View with errors on failure</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(UpsertRoomViewModel model, IFormFile? photo)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                if (model.IsEdit)
                {
                    // Update existing room
                    await _roomService.ValidateRoomOwnerAccessAsync(model.Id, UserId);

                    var room = await _roomService.GetRoomAsync(model.Id);
                    if (room == null)
                        return NotFound();

                    // Check name uniqueness (excluding current room)
                    var isUnique = await _roomService.IsNameUniqueInHouseholdAsync(
                        model.Name!, model.HouseholdId, model.Id);

                    if (!isUnique)
                    {
                        ModelState.AddModelError("Name", "A room with this name already exists in the household.");
                        return View(model);
                    }

                    room.Name = model.Name!;
                    room.Description = model.Description;
                    room.Priority = model.Priority;

                    await _roomService.UpdateRoomAsync(room, UserId);

                    // Handle photo upload for edit (will replace existing photo if any)
                    if (photo != null && photo.Length > 0)
                    {
                        try
                        {
                            await _roomService.UploadRoomPhotoAsync(model.Id, photo, UserId);
                            TempData["Success"] = "Room and photo updated successfully!";
                        }
                        catch (InvalidOperationException ex)
                        {
                            TempData["Warning"] = $"Room updated successfully, but photo upload failed: {ex.Message}";
                        }
                    }
                    else
                    {
                        TempData["Success"] = "Room updated successfully!";
                    }
                }
                else
                {
                    // Create new room
                    await _householdService.ValidateOwnerAccessAsync(model.HouseholdId, UserId);

                    // Check name uniqueness
                    var isUnique = await _roomService.IsNameUniqueInHouseholdAsync(
                        model.Name!, model.HouseholdId);

                    if (!isUnique)
                    {
                        ModelState.AddModelError("Name", "A room with this name already exists in the household.");
                        return View(model);
                    }

                    var room = await _roomService.CreateRoomAsync(
                        model.HouseholdId,
                        model.Name!,
                        model.Description,
                        model.Priority,
                        UserId);

                    // Handle photo upload for new room
                    if (photo != null && photo.Length > 0)
                    {
                        try
                        {
                            await _roomService.UploadRoomPhotoAsync(room.Id, photo, UserId);
                            TempData["Success"] = "Room created with photo successfully!";
                        }
                        catch (InvalidOperationException ex)
                        {
                            TempData["Warning"] = $"Room created successfully, but photo upload failed: {ex.Message}";
                        }
                    }
                    else
                    {
                        TempData["Success"] = "Room created successfully!";
                    }

                    return RedirectToAction("Details", new { id = room.Id });
                }

                return RedirectToAction("Details", new { id = model.Id });
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can create or edit rooms.";
                return RedirectToAction("Details", "Household", new { id = model.HouseholdId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Upsert for room {RoomId}", model.Id);
                ModelState.AddModelError("", "An error occurred while saving the room.");
                return View(model);
            }
        }

        /// <summary>
        /// POST: Room/UploadPhoto - Upload or replace room photo. Owner only.
        /// </summary>
        /// <param name="id">Room ID</param>
        /// <param name="photo">Photo file to upload</param>
        /// <returns>Redirect to Details</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(Guid id, IFormFile photo)
        {
            try
            {
                if (photo == null || photo.Length == 0)
                {
                    TempData["Error"] = "Please select a photo to upload.";
                    return RedirectToAction("Details", new { id });
                }

                var photoPath = await _roomService.UploadRoomPhotoAsync(id, photo, UserId);
                TempData["Success"] = "Room photo uploaded successfully!";
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can upload room photos.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading photo for room {RoomId}", id);
                TempData["Error"] = "An error occurred while uploading the photo.";
            }

            return RedirectToAction("Details", new { id });
        }

        /// <summary>
        /// POST: Room/DeletePhoto - Remove room photo from filesystem and database. Owner only.
        /// </summary>
        /// <param name="id">Room ID</param>
        /// <returns>Redirect to Details</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhoto(Guid id)
        {
            try
            {
                await _roomService.DeleteRoomPhotoAsync(id, UserId);
                TempData["Success"] = "Room photo deleted successfully!";
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can delete room photos.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo for room {RoomId}", id);
                TempData["Error"] = "An error occurred while deleting the photo.";
            }

            return RedirectToAction("Details", new { id });
        }

        /// <summary>
        /// GET: Room/Delete - Delete confirmation page showing room and associated tasks. Owner only.
        /// </summary>
        /// <param name="id">Room ID</param>
        /// <returns>View with room details for confirmation</returns>
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _roomService.ValidateRoomOwnerAccessAsync(id, UserId);

                var room = await _roomService.GetRoomWithTasksAsync(id);
                if (room == null)
                    return NotFound();

                return View(room);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can delete rooms.";
                return RedirectToAction("Details", new { id });
            }
        }

        /// <summary>
        /// POST: Room/Delete - Delete room permanently. Cascades to photo and associated tasks. Owner only.
        /// </summary>
        /// <param name="id">Room ID</param>
        /// <returns>Redirect to Room/Index for household</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                var room = await _roomService.GetRoomAsync(id);
                if (room == null)
                    return NotFound();

                var householdId = room.HouseholdId;

                await _roomService.DeleteRoomAsync(id, UserId);
                TempData["Success"] = "Room deleted successfully.";
                return RedirectToAction("Index", new { householdId });
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Only household owners can delete rooms.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting room {RoomId}", id);
                TempData["Error"] = "An error occurred while deleting the room.";
                return RedirectToAction("Details", new { id });
            }
        }
    }
}
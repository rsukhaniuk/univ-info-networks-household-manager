using HouseholdManager.Models.DTOs;
using HouseholdManager.Models.Entities;

namespace HouseholdManager.Models.ViewModels
{
    /// <summary>
    /// ViewModel for user dashboard containing statistics and household information
    /// </summary>
    public class DashboardViewModel
    {
        /// <summary>
        /// User's overall statistics (tasks completed, households count, etc.)
        /// </summary>
        public UserDashboardStats UserStats { get; set; } = new();

        /// <summary>
        /// All households where the user is a member
        /// </summary>
        public IReadOnlyList<Household> UserHouseholds { get; set; } = new List<Household>();

        /// <summary>
        /// Currently selected household (null if no household selected)
        /// </summary>
        public Household? CurrentHousehold { get; set; }

        /// <summary>
        /// Tasks assigned to the current user in the current household
        /// </summary>
        public IReadOnlyList<HouseholdTask> UserTasks { get; set; } = new List<HouseholdTask>();

        /// <summary>
        /// Recent task executions by the user in the current household
        /// </summary>
        public IReadOnlyList<TaskExecution> RecentExecutions { get; set; } = new List<TaskExecution>();

        /// <summary>
        /// Overdue tasks in the current household (for awareness)
        /// </summary>
        public IReadOnlyList<HouseholdTask> OverdueTasks { get; set; } = new List<HouseholdTask>();

        /// <summary>
        /// Check if user has selected a household
        /// </summary>
        public bool HasCurrentHousehold => CurrentHousehold != null;

        /// <summary>
        /// Check if user has any households
        /// </summary>
        public bool HasHouseholds => UserHouseholds.Count > 0;

        /// <summary>
        /// Check if user has tasks assigned
        /// </summary>
        public bool HasTasks => UserTasks.Count > 0;

        /// <summary>
        /// Check if there are overdue tasks
        /// </summary>
        public bool HasOverdueTasks => OverdueTasks.Count > 0;
    }
}

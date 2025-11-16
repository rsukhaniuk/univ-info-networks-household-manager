namespace HouseholdManager.Models.DTOs
{
    public class UserDashboardStats
    {
        public int TotalHouseholds { get; set; }
        public int OwnedHouseholds { get; set; }
        public int ActiveTasks { get; set; }
        public int CompletedTasksThisWeek { get; set; }
        public DateTime? LastActivity { get; set; }
    }
}

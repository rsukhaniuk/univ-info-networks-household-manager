using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Common
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

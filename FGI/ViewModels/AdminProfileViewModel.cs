using FGI.Models;

namespace FGI.ViewModels
{
    /// <summary>
    /// Admin user profile view model with system-wide KPIs and activities
    /// </summary>
    public class AdminProfileViewModel : BaseProfileViewModel
    {
        // Admin-specific KPIs
        public int TotalUsers { get; set; }
        public int TotalProjects { get; set; }
        public int TotalUnits { get; set; }
        public int TotalLeads { get; set; }
        public int AvailableUnits { get; set; }
        public int SoldUnits { get; set; }
        
        // Role breakdown
        public int MarketingUsers { get; set; }
        public int SalesUsers { get; set; }
        public int AdminUsers { get; set; }
        
        // System health metrics
        public double SystemUtilizationRate { get; set; }
        public int PendingAssignments { get; set; }
        public int UnassignedLeads { get; set; }
        
        // Recent activity - last 5 users added and lead assignments
        public List<RecentUserActivity> RecentUsers { get; set; } = new List<RecentUserActivity>();
        public List<RecentAssignmentActivity> RecentAssignments { get; set; } = new List<RecentAssignmentActivity>();
    }

    /// <summary>
    /// Represents a recent user activity for admin profile
    /// </summary>
    public class RecentUserActivity
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a recent lead assignment activity for admin profile
    /// </summary>
    public class RecentAssignmentActivity
    {
        public int LeadId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string FromSales { get; set; } = string.Empty;
        public string ToSales { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
    }
}






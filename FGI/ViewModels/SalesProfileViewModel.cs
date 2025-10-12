using FGI.Models;

namespace FGI.ViewModels
{
    /// <summary>
    /// Sales user profile view model with role-specific KPIs and activities
    /// </summary>
    public class SalesProfileViewModel : BaseProfileViewModel
    {
        // Sales-specific KPIs
        public int AssignedLeads { get; set; }
        public int LeadsConvertedToUnits { get; set; }
        public int UnitsSold { get; set; }
        public int ActiveTasks { get; set; }
        
        // Performance metrics
        public double LeadConversionRate { get; set; }
        public double UnitSalesRate { get; set; }
        public int CompletedTasks { get; set; }
        
        // Recent activity - last 5 assigned leads or tasks
        public List<RecentTaskActivity> RecentTasks { get; set; } = new List<RecentTaskActivity>();
    }

    /// <summary>
    /// Represents a recent task activity for display in sales profile
    /// </summary>
    public class RecentTaskActivity
    {
        public int LeadId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
        public bool IsUrgent { get; set; }
    }
}






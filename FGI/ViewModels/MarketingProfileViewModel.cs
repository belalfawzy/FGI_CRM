using FGI.Models;

namespace FGI.ViewModels
{
    /// <summary>
    /// Marketing user profile view model with role-specific KPIs and activities
    /// </summary>
    public class MarketingProfileViewModel : BaseProfileViewModel
    {
        // Marketing-specific KPIs
        public int LeadsCreated { get; set; }
        public int UnitsAdded { get; set; }
        public int FeedbacksGiven { get; set; }
        
        // Performance metrics
        public double LeadToUnitConversionRate { get; set; }
        public int ActiveLeads { get; set; }
        public int CompletedLeads { get; set; }
        
        // Recent activity - last 5 leads created by this user
        public List<RecentLeadActivity> RecentLeads { get; set; } = new List<RecentLeadActivity>();
    }

    /// <summary>
    /// Represents a recent lead activity for display in profile
    /// </summary>
    public class RecentLeadActivity
    {
        public int LeadId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
    }
}




using FGI.Enums;
using FGI.Models;

namespace FGI.ViewModel
{
    public class AdminLeadsViewModel
    {
        public List<Lead> Leads { get; set; }
        public List<User> SalesUsers { get; set; }
        public List<Project> Projects { get; set; }
        public Dictionary<int, List<LeadAssignmentHistory>> AssignmentHistories { get; set; }
        public Dictionary<int, List<LeadFeedback>> LeadFeedbacks { get; set; }

        // Filter properties
        public string SearchTerm { get; set; }
        public int? SelectedProjectId { get; set; }
        public int? SelectedSalesUserId { get; set; }
        public LeadStatusType? SelectedStatus { get; set; }
        public DateTime? FilterDate { get; set; }
        public bool UnassignedOnly
        {
            get; set;
        }
    }
}

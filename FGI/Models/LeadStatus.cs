using FGI.Enums;

namespace FGI.Models
{
    public class LeadStatus
    {
        public int Id { get; set; }
        public LeadStatusType status { get; set; }
        public ICollection<Lead> Leads { get; set; }
        public ICollection<LeadFeedback> Feedbacks { get; set; }
    }
}

using FGI.Enums;

namespace FGI.Models
{
    public class LeadFeedback
    {
        public int Id { get; set; }

        public int LeadId { get; set; }
        public Lead Lead { get; set; }

        public int SalesId { get; set; }
        public User Sales { get; set; }

        public LeadStatusType Status { get; set; }

        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}

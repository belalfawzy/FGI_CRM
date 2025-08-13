namespace FGI.Models
{
    public class LeadAssignmentHistory
    {
        public int Id { get; set; }

        public int LeadId { get; set; }
        public Lead Lead { get; set; }

        public int? FromSalesId { get; set; }
        public User FromSales { get; set; }

        public int ToSalesId { get; set; }
        public User ToSales { get; set; }

        public int ChangedById { get; set; } // Admin or Marketing
        public User ChangedBy { get; set; }

        public DateTime ChangedAt { get; set; }
    }

}

using FGI.Enums;
using System.ComponentModel.DataAnnotations;

namespace FGI.Models
{
    public class Lead
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Name is Required")]
        [StringLength(25, ErrorMessage = "Client Name must be less than 25 CH")]
        public string ClientName { get; set; }
        [Required]
        [Phone]
        public string ClientPhone { get; set; }
        public string? Comment { get; set; }
        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        public int? UnitId { get; set; }
        public Unit? Unit { get; set; }

        public int CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        public int? AssignedToId { get; set; }
        public User? AssignedTo { get; set; }
        public LeadStatusType CurrentStatus { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<LeadFeedback>? Feedbacks { get; set; }
        public ICollection<LeadAssignmentHistory>? AssignmentHistory { get; set; }
    }

}

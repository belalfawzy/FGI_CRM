using System.ComponentModel.DataAnnotations;

namespace FGI.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required(ErrorMessage ="Name is Required")]
        [StringLength(25,ErrorMessage ="FullName must be less than 25 CH")]
        public string FullName { get; set; }
        [Required(ErrorMessage ="Email is Required")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9]+\.(fgi)$", ErrorMessage = "Email must be in the format name@role.fgi")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; }
        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } // Admin, Marketing, Sales
        public ICollection<Lead>? CreatedLeads { get; set; }
        public ICollection<Lead>? AssignedLeads { get; set; }
        public ICollection<LeadAssignmentHistory>? ChangesMade { get; set; }
        public ICollection<LeadFeedback>? Feedbacks { get; set; }
    }
}

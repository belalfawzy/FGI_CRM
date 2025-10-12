using System.ComponentModel.DataAnnotations;

namespace FGI.ViewModels
{
    /// <summary>
    /// View model for user settings page
    /// </summary>
    public class UserSettingsViewModel
    {
        public int UserId { get; set; }
        
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(50, ErrorMessage = "Full name must be less than 50 characters")]
        public string FullName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
        
        public string Username { get; set; } = string.Empty; // Part before @
        public string Domain { get; set; } = string.Empty; // @role.fgi part
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}






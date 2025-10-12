using FGI.Models;

namespace FGI.ViewModels
{
    /// <summary>
    /// Base profile view model containing common information for all user roles
    /// </summary>
    public class BaseProfileViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        
        // System details
        public int TotalDaysInSystem { get; set; }
        public string AccountStatus { get; set; } = "Active";
    }
}






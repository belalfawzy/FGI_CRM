using FGI.ViewModels;

namespace FGI.Interfaces
{
    /// <summary>
    /// Interface for settings service providing user account management
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Updates user's full name
        /// Used in: SettingsController.UpdateProfile
        /// </summary>
        Task<bool> UpdateFullNameAsync(int userId, string fullName);
        
        /// <summary>
        /// Updates user's email username (part before @)
        /// Used in: SettingsController.UpdateEmail
        /// </summary>
        Task<bool> UpdateEmailUsernameAsync(int userId, string username);
        
        /// <summary>
        /// Updates user's password
        /// Used in: SettingsController.UpdatePassword
        /// </summary>
        Task<bool> UpdatePasswordAsync(int userId, string currentPassword, string newPassword);
        
        /// <summary>
        /// Gets user's current settings information
        /// Used in: SettingsController.Index
        /// </summary>
        Task<UserSettingsViewModel> GetUserSettingsAsync(int userId);
        
        /// <summary>
        /// Validates current password
        /// Used in: SettingsController for password verification
        /// </summary>
        Task<bool> ValidateCurrentPasswordAsync(int userId, string password);
    }
}






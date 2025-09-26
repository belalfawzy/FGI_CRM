using FGI.Interfaces;
using FGI.Models;
using FGI.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FGI.Services
{
    /// <summary>
    /// Service for managing user settings including email and password updates
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(AppDbContext context, ILogger<SettingsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Updates user's full name
        /// Used in: SettingsController.UpdateProfile
        /// </summary>
        public async Task<bool> UpdateFullNameAsync(int userId, string fullName)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                user.FullName = fullName.Trim();
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated full name for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating full name for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Updates user's email username (part before @)
        /// Used in: SettingsController.UpdateEmail
        /// </summary>
        public async Task<bool> UpdateEmailUsernameAsync(int userId, string username)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                // Extract domain from current email
                var currentEmail = user.Email;
                var domain = currentEmail.Substring(currentEmail.IndexOf('@'));
                
                // Validate that domain is @role.fgi format
                if (!domain.EndsWith(".fgi"))
                {
                    _logger.LogWarning("Invalid email domain for user {UserId}: {Domain}", userId, domain);
                    return false;
                }

                // Create new email with updated username
                var newEmail = $"{username.Trim().ToLower()}{domain}";
                
                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == newEmail && u.Id != userId);
                
                if (existingUser != null)
                {
                    _logger.LogWarning("Email {Email} already exists for another user", newEmail);
                    return false;
                }

                user.Email = newEmail;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated email username for user {UserId} to {Email}", userId, newEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email username for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Updates user's password
        /// Used in: SettingsController.UpdatePassword
        /// </summary>
        public async Task<bool> UpdatePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                // Validate current password
                if (user.PasswordHash != currentPassword)
                {
                    _logger.LogWarning("Invalid current password for user {UserId}", userId);
                    return false;
                }

                // Update password (keeping the same simple hash for now)
                user.PasswordHash = newPassword;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated password for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Gets user's current settings information
        /// Used in: SettingsController.Index
        /// </summary>
        public async Task<UserSettingsViewModel> GetUserSettingsAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    throw new ArgumentException("User not found");

                var email = user.Email;
                var atIndex = email.IndexOf('@');
                var username = atIndex > 0 ? email.Substring(0, atIndex) : email;
                var domain = atIndex > 0 ? email.Substring(atIndex) : "";

                return new UserSettingsViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Username = username,
                    Domain = domain,
                    Role = user.Role,
                    CreatedAt = DateTime.Now, // Placeholder - not stored in current model
                    LastLoginAt = DateTime.Now // Placeholder - not tracked in current model
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user settings for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Validates current password
        /// Used in: SettingsController for password verification
        /// </summary>
        public async Task<bool> ValidateCurrentPasswordAsync(int userId, string password)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                return user.PasswordHash == password;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating password for user {UserId}", userId);
                return false;
            }
        }
    }
}




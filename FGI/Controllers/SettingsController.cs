using FGI.Interfaces;
using FGI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FGI.Controllers
{
    /// <summary>
    /// Controller for managing user settings including email and password updates
    /// </summary>
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(ISettingsService settingsService, ILogger<SettingsController> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        /// <summary>
        /// Displays user settings page
        /// Route: /Settings
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return RedirectToAction("Login", "Account");

                var settings = await _settingsService.GetUserSettingsAsync(userId.Value);
                return View(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings for user {UserId}", GetCurrentUserId());
                TempData["ErrorMessage"] = "Error loading settings. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Updates user's full name
        /// Route: /Settings/UpdateProfile
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "User not authenticated" });

                if (string.IsNullOrWhiteSpace(fullName))
                    return Json(new { success = false, message = "Full name is required" });

                var success = await _settingsService.UpdateFullNameAsync(userId.Value, fullName);
                
                if (success)
                {
                    return Json(new { success = true, message = "Profile updated successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Error updating profile" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", GetCurrentUserId());
                return Json(new { success = false, message = "Error updating profile" });
            }
        }

        /// <summary>
        /// Updates user's email username
        /// Route: /Settings/UpdateEmail
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmail(string username)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "User not authenticated" });

                if (string.IsNullOrWhiteSpace(username))
                    return Json(new { success = false, message = "Username is required" });

                // Validate username format
                if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9._-]+$"))
                    return Json(new { success = false, message = "Username can only contain letters, numbers, dots, underscores, and hyphens" });

                var success = await _settingsService.UpdateEmailUsernameAsync(userId.Value, username);
                
                if (success)
                {
                    return Json(new { success = true, message = "Email updated successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Username already exists or invalid format" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email for user {UserId}", GetCurrentUserId());
                return Json(new { success = false, message = "Error updating email" });
            }
        }

        /// <summary>
        /// Updates user's password
        /// Route: /Settings/UpdatePassword
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(string currentPassword, string newPassword)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "User not authenticated" });

                if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                    return Json(new { success = false, message = "Current password and new password are required" });

                if (newPassword.Length < 6)
                    return Json(new { success = false, message = "New password must be at least 6 characters long" });

                var success = await _settingsService.UpdatePasswordAsync(userId.Value, currentPassword, newPassword);
                
                if (success)
                {
                    return Json(new { success = true, message = "Password updated successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Current password is incorrect" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password for user {UserId}", GetCurrentUserId());
                return Json(new { success = false, message = "Error updating password" });
            }
        }

        /// <summary>
        /// Helper method to get current user ID from claims
        /// </summary>
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }
    }
}




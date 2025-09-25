using FGI.Interfaces;
using FGI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FGI.Controllers
{
    /// <summary>
    /// Controller for managing user profiles with role-based KPIs and activities
    /// </summary>
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IProfileService _profileService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(IProfileService profileService, ILogger<ProfileController> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        /// <summary>
        /// Displays user profile based on their role
        /// Route: /Profile
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return RedirectToAction("Login", "Account");

                var userRole = User.FindFirstValue(ClaimTypes.Role);
                
                return userRole?.ToLower() switch
                {
                    "marketing" => View("MarketingProfile", await _profileService.GetMarketingProfileAsync(userId.Value)),
                    "sales" => View("SalesProfile", await _profileService.GetSalesProfileAsync(userId.Value)),
                    "admin" => View("AdminProfile", await _profileService.GetAdminProfileAsync(userId.Value)),
                    _ => View("BaseProfile", await _profileService.GetBaseProfileAsync(userId.Value))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile for user {UserId}", GetCurrentUserId());
                TempData["ErrorMessage"] = "Error loading profile. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }


        /// <summary>
        /// Gets profile data as JSON for AJAX requests
        /// Route: /Profile/GetProfileData
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProfileData()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "User not authenticated" });

                var userRole = User.FindFirstValue(ClaimTypes.Role);
                
                object profileData = userRole?.ToLower() switch
                {
                    "marketing" => await _profileService.GetMarketingProfileAsync(userId.Value),
                    "sales" => await _profileService.GetSalesProfileAsync(userId.Value),
                    "admin" => await _profileService.GetAdminProfileAsync(userId.Value),
                    _ => await _profileService.GetBaseProfileAsync(userId.Value)
                };

                return Json(new { success = true, data = profileData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile data for user {UserId}", GetCurrentUserId());
                return Json(new { success = false, message = "Error loading profile data" });
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

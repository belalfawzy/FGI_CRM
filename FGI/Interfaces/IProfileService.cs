using FGI.ViewModels;

namespace FGI.Interfaces
{
    /// <summary>
    /// Interface for profile service providing role-based user profile data
    /// </summary>
    public interface IProfileService
    {
        /// <summary>
        /// Gets marketing user profile with KPIs and recent activities
        /// </summary>
        Task<MarketingProfileViewModel> GetMarketingProfileAsync(int userId);
        
        /// <summary>
        /// Gets sales user profile with KPIs and recent activities
        /// </summary>
        Task<SalesProfileViewModel> GetSalesProfileAsync(int userId);
        
        /// <summary>
        /// Gets admin user profile with system-wide KPIs and activities
        /// </summary>
        Task<AdminProfileViewModel> GetAdminProfileAsync(int userId);
        
        /// <summary>
        /// Gets basic profile information for any user role
        /// </summary>
        Task<BaseProfileViewModel> GetBaseProfileAsync(int userId);
        
        /// <summary>
        /// Updates user profile information
        /// </summary>
        Task<bool> UpdateProfileAsync(int userId, string fullName, string email);
    }
}



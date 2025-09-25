using FGI.Interfaces;
using FGI.Models;
using FGI.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FGI.Services
{
    /// <summary>
    /// Service for managing user profiles with role-based KPIs and activities
    /// </summary>
    public class ProfileService : IProfileService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(AppDbContext context, ILogger<ProfileService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets marketing user profile with KPIs and recent activities
        /// Used in: ProfileController.Index for Marketing users
        /// </summary>
        public async Task<MarketingProfileViewModel> GetMarketingProfileAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.CreatedLeads)
                        .ThenInclude(l => l.Project)
                    .Include(u => u.CreatedLeads)
                        .ThenInclude(l => l.Unit)
                    .Include(u => u.Feedbacks)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    throw new ArgumentException("User not found");

                var profile = new MarketingProfileViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedAt = DateTime.Now, // Assuming user creation date is not stored
                    LastLoginAt = DateTime.Now, // Assuming last login is not tracked
                    TotalDaysInSystem = (DateTime.Now - DateTime.Now).Days, // Placeholder
                    AccountStatus = "Active"
                };

                // Calculate Marketing-specific KPIs
                var leadsCreated = await _context.Leads
                    .Where(l => l.CreatedById == userId)
                    .ToListAsync();

                var unitsAdded = await _context.Units
                    .Where(u => u.CreatedById == userId)
                    .CountAsync();

                var feedbacksGiven = await _context.LeadFeedbacks
                    .Where(f => f.SalesId == userId)
                    .CountAsync();

                profile.LeadsCreated = leadsCreated.Count;
                profile.UnitsAdded = unitsAdded;
                profile.FeedbacksGiven = feedbacksGiven;

                // Calculate performance metrics
                profile.LeadToUnitConversionRate = leadsCreated.Count > 0 
                    ? (double)unitsAdded / leadsCreated.Count * 100 
                    : 0;

                profile.ActiveLeads = leadsCreated.Count(l => l.CurrentStatus != FGI.Enums.LeadStatusType.DoneDeal && 
                                                           l.CurrentStatus != FGI.Enums.LeadStatusType.Canceled &&
                                                           l.CurrentStatus != FGI.Enums.LeadStatusType.Closed);
                
                profile.CompletedLeads = leadsCreated.Count(l => l.CurrentStatus == FGI.Enums.LeadStatusType.DoneDeal);

                // Get recent leads (last 5)
                profile.RecentLeads = leadsCreated
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(5)
                    .Select(l => new RecentLeadActivity
                    {
                        LeadId = l.Id,
                        ClientName = l.ClientName,
                        ClientPhone = l.ClientPhone,
                        ProjectName = l.Project?.Name ?? "No Project",
                        Status = l.CurrentStatus.ToString(),
                        CreatedAt = l.CreatedAt,
                        TimeAgo = GetTimeAgo(l.CreatedAt)
                    })
                    .ToList();

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting marketing profile for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Gets sales user profile with KPIs and recent activities
        /// Used in: ProfileController.Index for Sales users
        /// </summary>
        public async Task<SalesProfileViewModel> GetSalesProfileAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.AssignedLeads)
                        .ThenInclude(l => l.Project)
                    .Include(u => u.AssignedLeads)
                        .ThenInclude(l => l.Unit)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    throw new ArgumentException("User not found");

                var profile = new SalesProfileViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedAt = DateTime.Now, // Placeholder
                    LastLoginAt = DateTime.Now, // Placeholder
                    TotalDaysInSystem = (DateTime.Now - DateTime.Now).Days, // Placeholder
                    AccountStatus = "Active"
                };

                // Calculate Sales-specific KPIs
                var assignedLeads = await _context.Leads
                    .Where(l => l.AssignedToId == userId)
                    .ToListAsync();

                var leadsConvertedToUnits = await _context.Leads
                    .Where(l => l.AssignedToId == userId && l.UnitId != null)
                    .CountAsync();

                var unitsSold = await _context.Units
                    .Where(u => u.CreatedById == userId && !u.IsAvailable)
                    .CountAsync();

                var activeTasks = await _context.Leads
                    .Where(l => l.AssignedToId == userId && 
                               l.CurrentStatus != FGI.Enums.LeadStatusType.DoneDeal &&
                               l.CurrentStatus != FGI.Enums.LeadStatusType.Canceled &&
                               l.CurrentStatus != FGI.Enums.LeadStatusType.Closed)
                    .CountAsync();

                profile.AssignedLeads = assignedLeads.Count;
                profile.LeadsConvertedToUnits = leadsConvertedToUnits;
                profile.UnitsSold = unitsSold;
                profile.ActiveTasks = activeTasks;

                // Calculate performance metrics
                profile.LeadConversionRate = assignedLeads.Count > 0 
                    ? (double)leadsConvertedToUnits / assignedLeads.Count * 100 
                    : 0;

                profile.UnitSalesRate = leadsConvertedToUnits > 0 
                    ? (double)unitsSold / leadsConvertedToUnits * 100 
                    : 0;

                profile.CompletedTasks = assignedLeads.Count - activeTasks;

                // Get recent tasks (last 5)
                profile.RecentTasks = assignedLeads
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(5)
                    .Select(l => new RecentTaskActivity
                    {
                        LeadId = l.Id,
                        ClientName = l.ClientName,
                        ClientPhone = l.ClientPhone,
                        ProjectName = l.Project?.Name ?? "No Project",
                        Status = l.CurrentStatus.ToString(),
                        AssignedAt = l.CreatedAt,
                        LastActivityAt = l.UpdatedAt,
                        TimeAgo = GetTimeAgo(l.CreatedAt),
                        IsUrgent = l.CurrentStatus == FGI.Enums.LeadStatusType.New
                    })
                    .ToList();

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales profile for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Gets admin user profile with system-wide KPIs and activities
        /// Used in: ProfileController.Index for Admin users
        /// </summary>
        public async Task<AdminProfileViewModel> GetAdminProfileAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    throw new ArgumentException("User not found");

                var profile = new AdminProfileViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedAt = DateTime.Now, // Placeholder
                    LastLoginAt = DateTime.Now, // Placeholder
                    TotalDaysInSystem = (DateTime.Now - DateTime.Now).Days, // Placeholder
                    AccountStatus = "Active"
                };

                // Calculate Admin-specific KPIs
                profile.TotalUsers = await _context.Users.CountAsync();
                profile.TotalProjects = await _context.Projects.CountAsync();
                profile.TotalUnits = await _context.Units.CountAsync();
                profile.TotalLeads = await _context.Leads.CountAsync();
                profile.AvailableUnits = await _context.Units.CountAsync(u => u.IsAvailable);
                profile.SoldUnits = await _context.Units.CountAsync(u => !u.IsAvailable);

                // Role breakdown
                profile.MarketingUsers = await _context.Users.CountAsync(u => u.Role == "Marketing");
                profile.SalesUsers = await _context.Users.CountAsync(u => u.Role == "Sales");
                profile.AdminUsers = await _context.Users.CountAsync(u => u.Role == "Admin");

                // System health metrics
                profile.UnassignedLeads = await _context.Leads.CountAsync(l => l.AssignedToId == null);
                profile.PendingAssignments = await _context.Leads
                    .CountAsync(l => l.AssignedToId != null && l.CurrentStatus == FGI.Enums.LeadStatusType.New);

                profile.SystemUtilizationRate = profile.TotalUnits > 0 
                    ? (double)profile.SoldUnits / profile.TotalUnits * 100 
                    : 0;

                // Get recent users (last 5)
                var recentUsers = await _context.Users
                    .OrderByDescending(u => u.Id) // Assuming higher ID means newer user
                    .Take(5)
                    .Select(u => new RecentUserActivity
                    {
                        UserId = u.Id,
                        FullName = u.FullName,
                        Email = u.Email,
                        Role = u.Role,
                        CreatedAt = DateTime.Now, // Placeholder
                        TimeAgo = "Recently"
                    })
                    .ToListAsync();

                profile.RecentUsers = recentUsers;

                // Get recent assignments (last 5) - with error handling
                List<RecentAssignmentActivity> recentAssignments = new List<RecentAssignmentActivity>();
                try
                {
                    recentAssignments = await _context.LeadAssignmentHistories
                        .Include(h => h.Lead)
                        .Include(h => h.FromSales)
                        .Include(h => h.ToSales)
                        .Include(h => h.ChangedBy)
                        .OrderByDescending(h => h.ChangedAt)
                        .Take(5)
                        .Select(h => new RecentAssignmentActivity
                        {
                            LeadId = h.LeadId,
                            ClientName = h.Lead.ClientName,
                            FromSales = h.FromSales != null ? h.FromSales.FullName : "Unassigned",
                            ToSales = h.ToSales != null ? h.ToSales.FullName : "Unassigned",
                            ChangedBy = h.ChangedBy.FullName,
                            AssignedAt = h.ChangedAt,
                            TimeAgo = GetTimeAgo(h.ChangedAt)
                        })
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load recent assignments for admin profile");
                    // Continue with empty list if assignments can't be loaded
                }

                profile.RecentAssignments = recentAssignments;

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin profile for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Gets basic profile information for any user role
        /// Used in: ProfileController for basic profile display
        /// </summary>
        public async Task<BaseProfileViewModel> GetBaseProfileAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    throw new ArgumentException("User not found");

                return new BaseProfileViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedAt = DateTime.Now, // Placeholder
                    LastLoginAt = DateTime.Now, // Placeholder
                    TotalDaysInSystem = (DateTime.Now - DateTime.Now).Days, // Placeholder
                    AccountStatus = "Active"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting base profile for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Updates user profile information
        /// Used in: ProfileController.UpdateProfile
        /// </summary>
        public async Task<bool> UpdateProfileAsync(int userId, string fullName, string email)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                user.FullName = fullName;
                user.Email = email;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Helper method to calculate time ago string
        /// </summary>
        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;
            
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} days ago";
            else if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours} hours ago";
            else if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            else
                return "Just now";
        }
    }
}

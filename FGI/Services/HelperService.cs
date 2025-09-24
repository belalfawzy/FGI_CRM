using FGI.Enums;
using FGI.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace FGI.Services
{
    public class HelperService : IHelperService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HelperService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userId, out int id) ? id : null;
        }

        public bool IsAjaxRequest()
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["X-Requested-With"].ToString() == "XMLHttpRequest";
        }

        public string NormalizePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            // Remove all non-digit characters
            var normalized = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Remove leading zeros and country codes (assuming Egyptian numbers)
            // For Egypt: numbers typically start with 1 after removing +20 or 0
            if (normalized.StartsWith("20")) // Egypt country code without +
                normalized = normalized.Substring(2);

            if (normalized.StartsWith("0"))
                normalized = normalized.Substring(1);

            return normalized;
        }

        public string GetStatusDisplayName(LeadStatusType status)
        {
            return status switch
            {
                LeadStatusType.New => "New Lead",
                LeadStatusType.NoAnswer => "No Answer",
                LeadStatusType.FollowUp => "Follow Up",
                LeadStatusType.Busy => "Busy",
                LeadStatusType.Canceled => "Canceled",
                LeadStatusType.DoneDeal => "Done Deal",
                LeadStatusType.NotInterested => "Not Interested",
                LeadStatusType.WrongNumber => "Wrong Number",
                LeadStatusType.Closed => "Closed",
                LeadStatusType.Potential => "Potential",
                LeadStatusType.NoBudget => "No Budget",
                _ => ""
            };
        }
    }
}

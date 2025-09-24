using System.Security.Claims;

namespace FGI.Interfaces
{
    public interface IHelperService
    {
        int? GetCurrentUserId();
        bool IsAjaxRequest();
        string NormalizePhoneNumber(string phoneNumber);
        string GetStatusDisplayName(FGI.Enums.LeadStatusType status);
    }
}

using FGI.Enums;
using FGI.Models;

namespace FGI.Interfaces
{
    public interface ILeadFeedbackService
    {
        Task AddFeedbackAsync(int leadId, int salesId, LeadStatusType status, string comment);
        Task<List<LeadFeedback>> GetFeedbacksByLeadAsync(int leadId);

    }
}

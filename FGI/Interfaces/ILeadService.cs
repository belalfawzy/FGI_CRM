using FGI.Enums;
using FGI.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace FGI.Interfaces
{
    public interface ILeadService
    {
        Task CreateLeadAsync(Lead lead, int createdById);
        Task<List<Lead>> GetLeadsForUserAsync(User user);
        Task AssignLeadAsync(int leadId, int toSalesId, int changedById);
        Task ReassignLeadAsync(int leadId, int newSalesId, int changedById);
        Task UpdateStatusAsync(int leadId, LeadStatusType status);
        Task DeleteLeadAsync(int leadId);
        Task<Lead> GetLeadByIdAsync(int id);
        Task<List<Lead>> GetAllLeadsAsync();
        Task UpdateLeadAsync(Lead lead);
        Task<List<Lead>> GetLeadsBySalesPersonAsync(int salesPersonId);
        Task<List<Lead>> GetActiveLeadsBySalesPersonAsync(int salesPersonId);
        Task<bool> LeadExists(int id);

    }
}

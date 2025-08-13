using FGI.Enums;
using FGI.Interfaces;
using FGI.Models;
using Microsoft.EntityFrameworkCore;

namespace FGI.Services
{
    public class LeadFeedbackService : ILeadFeedbackService
    {
        private readonly AppDbContext _context;

        public LeadFeedbackService(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddFeedbackAsync(int leadId, int salesId, LeadStatusType status, string comment)
        {
            var feedback = new LeadFeedback
            {
                LeadId = leadId,
                SalesId = salesId,
                Status = status,
                Comment = comment,
                CreatedAt = DateTime.Now
            };

            _context.LeadFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();
        }

        public async Task<List<LeadFeedback>> GetFeedbacksByLeadAsync(int leadId)
        {
            return await _context.LeadFeedbacks
                .Where(f => f.LeadId == leadId)
                .Include(f => f.Sales)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }
    }
}

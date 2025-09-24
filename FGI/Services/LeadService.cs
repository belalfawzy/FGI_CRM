using FGI.Controllers;
using FGI.Enums;
using FGI.Interfaces;
using FGI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace FGI.Services
{
    public class LeadService : ILeadService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<LeadController> _logger;

        public LeadService(AppDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<LeadController> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task CreateLeadAsync(Lead lead, int createdById)
        {
            // التحقق من أن الوحدة موجودة
            var unit = await _context.Units.FindAsync(lead.UnitId);
            if (unit == null)
            {
                throw new Exception("الوحدة المحددة غير موجودة");
            }

            // إذا كان هناك مشروع، التحقق من وجوده
            if (lead.ProjectId.HasValue)
            {
                var projectExists = await _context.Projects.AnyAsync(p => p.Id == lead.ProjectId);
                if (!projectExists)
                {
                    throw new Exception("المشروع المحدد غير موجود");
                }
            }

            // تعيين القيم المطلوبة للـ Lead
            lead.CreatedById = createdById;
            lead.CreatedAt = DateTime.Now;
            lead.CurrentStatus = LeadStatusType.New;

            // إضافة الـ Lead إلى قاعدة البيانات
            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Lead>> GetLeadsForUserAsync(User user)
        {
            if (user.Role == "Admin" || user.Role == "MarketingLeader")
            {
                return await _context.Leads
                    .Include(l => l.Project)
                    .Include(l => l.AssignedTo)
                    .ToListAsync();
            }

            if (user.Role == "Marketing")
            {
                return await _context.Leads
                    .Where(l => l.CreatedById == user.Id)
                    .Include(l => l.Project)
                    .Include(l => l.AssignedTo)
                    .ToListAsync();
            }

            // Sales
            return await _context.Leads
                .Where(l => l.AssignedToId == user.Id)
                .Include(l => l.Project)
                .Include(l => l.Unit)
                .ToListAsync();
        }

        public async Task AssignLeadAsync(int leadId, int toSalesId, int changedById)
        {
            var lead = await _context.Leads.FindAsync(leadId);
            if (lead == null) return;

            var history = new LeadAssignmentHistory
            {
                LeadId = leadId,
                FromSalesId = lead.AssignedToId,
                ToSalesId = toSalesId,
                ChangedById = changedById,
                ChangedAt = DateTime.Now
            };

            lead.AssignedToId = toSalesId;
            lead.UpdatedAt = DateTime.Now;

            _context.LeadAssignmentHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        public async Task ReassignLeadAsync(int leadId, int newSalesId, int changedById)
        {
            var lead = await _context.Leads.FindAsync(leadId);
            if (lead == null) return;

            var history = new LeadAssignmentHistory
            {
                LeadId = leadId,
                FromSalesId = lead.AssignedToId,
                ToSalesId = newSalesId,
                ChangedById = changedById,
                ChangedAt = DateTime.Now
            };

            lead.AssignedToId = newSalesId;
            lead.UpdatedAt = DateTime.Now;

            _context.LeadAssignmentHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(int leadId, LeadStatusType status)
        {
            var lead = await _context.Leads.FindAsync(leadId);
            if (lead == null) return;

            lead.CurrentStatus = status;
            lead.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteLeadAsync(int leadId)
        {
            var lead = await _context.Leads.FindAsync(leadId);
            if (lead == null) return;

            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();
        }

        public async Task<Lead> GetLeadByIdAsync(int id)
        {
            return await _context.Leads
                .Include(l => l.Project)
                .Include(l => l.Unit)
                .Include(l => l.CreatedBy)
                .Include(l => l.AssignedTo)
                .FirstOrDefaultAsync(l => l.Id == id);
        }
        public async Task<List<Lead>> GetAllLeadsAsync()
        {
            return await _context.Leads
                .Include(l => l.Project)
                .Include(l => l.Unit)
                .Include(l => l.CreatedBy)
                .Include(l => l.AssignedTo)
                .ToListAsync();
        }
        public async Task UpdateLeadAsync(Lead lead)
        {
            // Get the original lead including the Unit
            var originalLead = await _context.Leads
                .Include(l => l.Unit)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == lead.Id);

            // Handle assignment changes
            if (originalLead?.AssignedToId != lead.AssignedToId)
            {
                var history = new LeadAssignmentHistory
                {
                    LeadId = lead.Id,
                    FromSalesId = originalLead?.AssignedToId,
                    ToSalesId = (int)lead.AssignedToId,
                    ChangedById = int.Parse(_httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)),
                    ChangedAt = DateTime.Now
                };
                _context.LeadAssignmentHistories.Add(history);
            }

            // Check if status changed to DoneDeal and unit exists
            if (originalLead?.CurrentStatus != LeadStatusType.DoneDeal &&
                lead.CurrentStatus == LeadStatusType.DoneDeal &&
                lead.UnitId.HasValue)
            {
                var unit = await _context.Units.FindAsync(lead.UnitId.Value);
                if (unit != null)
                {
                    unit.IsAvailable = false;
                    _context.Units.Update(unit);
                }
            }

            lead.UpdatedAt = DateTime.Now;
            _context.Leads.Update(lead);
            await _context.SaveChangesAsync();
        }
        public async Task<List<Lead>> GetLeadsBySalesPersonAsync(int salesPersonId)
        {
            return await _context.Leads
                .Where(l => l.AssignedToId == salesPersonId)
                .Include(l => l.Project)
                .Include(l => l.Unit)
                .ToListAsync();
        }
        public async Task<bool> LeadExists(int id)
        {
            return await _context.Leads.AnyAsync(e => e.Id == id);
        }

        public async Task<List<Lead>> GetActiveLeadsBySalesPersonAsync(int salesPersonId)
        {
            return await _context.Leads
                .Include(l => l.Project)
                .Include(l => l.Unit)
                .Include(l => l.AssignedTo)
                .Where(l => l.AssignedToId == salesPersonId &&
                           l.CurrentStatus != LeadStatusType.DoneDeal &&
                           l.CurrentStatus != LeadStatusType.Canceled)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task DistributeLeadsAsync(string distributionMethod, int changedById)
        {
            var unassignedLeads = await _context.Leads
                .Where(l => l.AssignedToId == null)
                .OrderBy(l => l.CreatedAt)
                .ToListAsync();

            var salesUsers = await _context.Users
                .Where(u => u.Role == UserRole.Sales.ToString())
                .ToListAsync();

            if (!unassignedLeads.Any() || !salesUsers.Any())
                return;

            int leadsPerSales = unassignedLeads.Count / salesUsers.Count;
            int remainingLeads = unassignedLeads.Count % salesUsers.Count;

            int leadIndex = 0;
            foreach (var salesUser in salesUsers)
            {
                int leadsToAssign = leadsPerSales;

                if (remainingLeads > 0)
                {
                    leadsToAssign++;
                    remainingLeads--;
                }

                for (int i = 0; i < leadsToAssign && leadIndex < unassignedLeads.Count; i++)
                {
                    var lead = unassignedLeads[leadIndex];
                    lead.AssignedToId = salesUser.Id;

                    var history = new LeadAssignmentHistory
                    {
                        LeadId = lead.Id,
                        FromSalesId = null,
                        ToSalesId = salesUser.Id,
                        ChangedById = changedById,
                        ChangedAt = DateTime.Now
                    };

                    _context.LeadAssignmentHistories.Add(history);
                    leadIndex++;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<byte[]> ExportLeadsToCsvAsync()
        {
            var leads = await _context.Leads
                .Include(l => l.Project)
                .Include(l => l.Unit)
                .Include(l => l.CreatedBy)
                .Include(l => l.AssignedTo)
                .ToListAsync();

            var builder = new StringBuilder();
            builder.AppendLine("Client Name,Client Phone,Project,Unit Code,Status,Created By,Assigned To,Last Updated");

            foreach (var lead in leads.OrderByDescending(l => l.UpdatedAt))
            {
                var clientName = $"\"{lead.ClientName?.Replace("\"", "\"\"")}\"";
                var clientPhone = lead.ClientPhone ?? "";
                var projectName = $"\"{lead.Project?.Name?.Replace("\"", "\"\"") ?? ""}\"";
                var unitCode = lead.Unit?.UnitCode ?? "";
                var status = GetStatusDisplayName(lead.CurrentStatus);
                var createdBy = $"\"{lead.CreatedBy?.FullName?.Replace("\"", "\"\"") ?? ""}\"";
                var assignedTo = $"\"{lead.AssignedTo?.FullName?.Replace("\"", "\"\"") ?? "Unassigned"}\"";
                var lastUpdated = lead.UpdatedAt?.ToString("dd MMM yyyy HH:mm") ?? "";

                builder.AppendLine($"{clientName},{clientPhone},{projectName},{unitCode},{status},{createdBy},{assignedTo},{lastUpdated}");
            }

            return Encoding.UTF8.GetBytes(builder.ToString());
        }

        public async Task<object> SearchClientAsync(string term)
        {
            var normalizedPhone = NormalizePhoneNumber(term);

            if (!string.IsNullOrEmpty(normalizedPhone))
            {
                var allLeads = await _context.Leads.ToListAsync();

                var matchingLead = allLeads.FirstOrDefault(l =>
                    !string.IsNullOrEmpty(l.ClientPhone) &&
                    NormalizePhoneNumber(l.ClientPhone) == normalizedPhone);

                if (matchingLead != null)
                {
                    return new
                    {
                        found = true,
                        id = matchingLead.Id,
                        name = matchingLead.ClientName,
                        phone = matchingLead.ClientPhone,
                        searchType = "phone"
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(term))
            {
                var leadByName = await _context.Leads
                    .Where(l => l.ClientName.Contains(term))
                    .FirstOrDefaultAsync();

                if (leadByName != null)
                {
                    return new
                    {
                        found = true,
                        id = leadByName.Id,
                        name = leadByName.ClientName,
                        phone = leadByName.ClientPhone,
                        searchType = "name"
                    };
                }
            }

            return new { found = false };
        }

        public async Task<object> SearchOwnerAsync(string term)
        {
            var normalizedPhone = NormalizePhoneNumber(term);

            if (!string.IsNullOrEmpty(normalizedPhone))
            {
                var owners = await _context.Owners.ToListAsync();

                var matchingOwner = owners.FirstOrDefault(o =>
                    !string.IsNullOrEmpty(o.Phone) &&
                    NormalizePhoneNumber(o.Phone) == normalizedPhone);

                if (matchingOwner != null)
                {
                    return new
                    {
                        found = true,
                        id = matchingOwner.Id,
                        name = matchingOwner.Name,
                        phone = matchingOwner.Phone,
                        email = matchingOwner.Email,
                        searchType = "phone"
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(term))
            {
                var ownerByName = await _context.Owners
                    .Where(o => o.Name.Contains(term))
                    .FirstOrDefaultAsync();

                if (ownerByName != null)
                {
                    return new
                    {
                        found = true,
                        id = ownerByName.Id,
                        name = ownerByName.Name,
                        phone = ownerByName.Phone,
                        email = ownerByName.Email,
                        searchType = "name"
                    };
                }
            }

            return new { found = false };
        }

        public async Task<object> AddOwnerAsync(string name, string phone, string email)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new { success = false, message = "Owner name is required" };
            }

            var owner = new Owner
            {
                Name = name,
                Phone = phone,
                Email = email,
                CreatedAt = DateTime.Now
            };

            _context.Owners.Add(owner);
            await _context.SaveChangesAsync();

            return new
            {
                success = true,
                ownerId = owner.Id,
                message = "Owner added successfully"
            };
        }

        private string NormalizePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            var normalized = new string(phoneNumber.Where(char.IsDigit).ToArray());

            if (normalized.StartsWith("20"))
                normalized = normalized.Substring(2);

            if (normalized.StartsWith("0"))
                normalized = normalized.Substring(1);

            return normalized;
        }

        private string GetStatusDisplayName(LeadStatusType status)
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

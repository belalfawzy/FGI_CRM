using FGI.Interfaces;
using FGI.Models;
using FGI.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;

namespace FGI.Services
{
    public class UnitService : IUnitService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UnitService> _logger;

        public UnitService(AppDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<UnitService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<List<Unit>> GetUnitsByProjectIdAsync(int projectId)
        {
            return await _context.Units
                .Where(u => u.ProjectId == projectId)
                .OrderBy(u => u.UnitCode) 
                .ToListAsync();
        }
        public async Task<bool> UnitCodeExists(string unitCode, int? projectId)
        {
            // If no project is selected or no unit code provided, it can't exist
            if (projectId == null || string.IsNullOrWhiteSpace(unitCode))
            {
                return false;
            }

            return await _context.Units
                .AnyAsync(u => u.UnitCode == unitCode && u.ProjectId == projectId);
        }

        public async Task<Unit> AddUnitAsync(Unit unit)
        {
            // التحقق من صحة المودل قبل الحفظ
            var validationContext = new ValidationContext(unit);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(unit, validationContext, validationResults, true);

            if (!isValid)
            {
                var errorMessages = validationResults.Select(vr => vr.ErrorMessage);
                throw new InvalidOperationException(string.Join("; ", errorMessages));
            }

            // التحقق من تكرار كود الوحدة إذا كان موجوداً
            if (!string.IsNullOrWhiteSpace(unit.UnitCode) && unit.ProjectId.HasValue)
            {
                if (await UnitCodeExists(unit.UnitCode, unit.ProjectId.Value))
                {
                    throw new InvalidOperationException("Unit code already exists for this project.");
                }
            }

            // تعيين المستخدم الذي أضاف الوحدة
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userId, out int createdById))
            {
                unit.CreatedById = createdById;
            }

            unit.CreatedAt = DateTime.Now;

            _context.Units.Add(unit);
            await _context.SaveChangesAsync();
            
            // Reload the unit with Owner information
            return await _context.Units
                .Include(u => u.Owner)
                .Include(u => u.Project)
                .Include(u => u.CreatedBy)
                .FirstOrDefaultAsync(u => u.Id == unit.Id);
        }

        public async Task UpdateAvailabilityAsync(int unitId, bool isAvailable)
        {
            var unit = await _context.Units.FindAsync(unitId);
            if (unit != null)
            {
                unit.IsAvailable = isAvailable;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteUnitAsync(int unitId)
        {
            var unit = await _context.Units.FindAsync(unitId);
            if (unit != null)
            {
                _context.Units.Remove(unit);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<Unit> GetUnitByIdAsync(int unitId)
        {
            return await _context.Units
                .Include(u => u.Owner)
                .Include(u => u.Project)
                .Include(u => u.CreatedBy)
                .FirstOrDefaultAsync(u => u.Id == unitId);
        }
        public async Task<List<Unit>> GetUnitsByCreatorAsync(int creatorId)
        {
            return await _context.Units
                .Include(u => u.Project)
                .Where(u => u.CreatedById == creatorId)
                .OrderBy(u => u.UnitCode)
                .Include(u=>u.Owner)
                .ToListAsync();
        }
        public async Task UpdateUnitAsync(Unit unit)
        {
            try
            {
                var existingUnit = await _context.Units.FindAsync(unit.Id);
                if (existingUnit == null)
                {
                    throw new KeyNotFoundException("Unit not found");
                }

                // Save the original created values
                var createdById = existingUnit.CreatedById;
                var createdAt = existingUnit.CreatedAt;

                // Update all properties except CreatedById and CreatedAt
                _context.Entry(existingUnit).CurrentValues.SetValues(unit);

                // Restore the original created values
                existingUnit.CreatedById = createdById;
                existingUnit.CreatedAt = createdAt;

                // Handle navigation properties
                existingUnit.ProjectId = unit.ProjectId;
                existingUnit.OwnerId = unit.OwnerId;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating unit");
                throw;
            }
        }
        public async Task<IEnumerable<Unit>> GetAvailableUnitsAsync()
        {
            return await _context.Units
                .Where(u => u.IsAvailable)
                .ToListAsync();
        }

        public async Task<object> GetUnitsForSelectAsync(int projectId, string term)
        {
            IEnumerable<Unit> units;

            if (projectId > 0)
            {
                units = await GetUnitsByProjectIdAsync(projectId);
            }
            else
            {
                units = await GetAvailableUnitsAsync();
            }

            if (!string.IsNullOrWhiteSpace(term))
            {
                term = term.Trim();
                units = units.Where(u =>
                    (!string.IsNullOrWhiteSpace(u.UnitCode) && u.UnitCode.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(u.Location) && u.Location.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(u.Description) && u.Description.Contains(term, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            var results = units.OrderBy(u => u.UnitCode).Select(u => new
            {
                id = u.Id,
                text = $"{(string.IsNullOrWhiteSpace(u.UnitCode) ? "NA" : u.UnitCode)} - {(string.IsNullOrWhiteSpace(u.Location) ? "NA" : u.Location)}",
                disabled = !u.IsAvailable,
                projectId = u.ProjectId,
                unit = new
                {
                    UnitCode = string.IsNullOrWhiteSpace(u.UnitCode) ? "NA" : u.UnitCode,
                    UnitType = u.Type,
                    UnitSaleType = u.UnitType,
                    Price = u.Price,
                    Area = u.Area
                }
            }).ToList();

            return results;
        }

        public async Task<List<Unit>> GetFilteredUnitsAsync(UnitType? type, int? projectId, decimal? minPrice, decimal? maxPrice, int? bedrooms, bool? isAvailable, decimal? minArea, int? bathrooms, UnitSaleType? saleType, string searchTerm)
        {
            var query = _context.Units
                .Include(u => u.Project)
                .Include(u => u.Owner)
                .AsQueryable();

            if (isAvailable.HasValue)
                query = query.Where(u => u.IsAvailable == isAvailable.Value);

            if (type.HasValue)
                query = query.Where(u => u.Type == type.Value);

            if (projectId.HasValue)
                query = query.Where(u => u.ProjectId == projectId.Value);

            if (minPrice.HasValue)
                query = query.Where(u => u.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(u => u.Price <= maxPrice.Value);

            if (bedrooms.HasValue)
                query = bedrooms.Value == 4 ?
                    query.Where(u => u.Bedrooms >= 4) :
                    query.Where(u => u.Bedrooms == bedrooms.Value);

            if (minArea.HasValue)
                query = query.Where(u => u.Area >= minArea.Value);

            if (bathrooms.HasValue)
                query = bathrooms.Value == 3 ?
                    query.Where(u => u.Bathrooms >= 3) :
                    query.Where(u => u.Bathrooms == bathrooms.Value);

            if (saleType.HasValue)
                query = query.Where(u => u.UnitType == saleType.Value);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalizedSearchTerm = searchTerm.Trim().ToLower();
                query = query.Where(u =>
                    (u.UnitCode != null && u.UnitCode.ToLower().Contains(normalizedSearchTerm)) ||
                    (u.Owner != null && u.Owner.Phone != null && u.Owner.Phone.Contains(normalizedSearchTerm))
                );
            }

            return await query
                .OrderByDescending(u => u.CreatedAt)
                .ThenBy(u => u.Project.Name)
                .ThenBy(u => u.UnitCode)
                .ToListAsync();
        }

        public async Task<List<Unit>> GetAllUnitsAsync()
        {
            return await _context.Units
                .Include(u => u.Project)
                .Include(u => u.Owner)
                .Include(u => u.CreatedBy)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<byte[]> ExportUnitsToCsvAsync(List<Unit> units)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Unit Code,Project,Location,Type,Sale Type,Price,Currency,Area,Bedrooms,Bathrooms,Owner,Owner Phone,Owner Email,Is Available,Description,Created By,Created At");

            foreach (var unit in units.OrderByDescending(u => u.CreatedAt))
            {
                var unitCode = $"\"{unit.UnitCode?.Replace("\"", "\"\"") ?? ""}\"";
                var projectName = $"\"{unit.Project?.Name?.Replace("\"", "\"\"") ?? ""}\"";
                var location = $"\"{unit.Location?.Replace("\"", "\"\"") ?? ""}\"";
                var type = unit.Type.ToString();
                var saleType = unit.UnitType.ToString();
                var price = unit.Price.ToString("N0");
                var currency = unit.Currency.ToString();
                var area = unit.Area.ToString();
                var bedrooms = unit.Bedrooms.ToString();
                var bathrooms = unit.Bathrooms.ToString();
                var ownerName = $"\"{unit.Owner?.Name?.Replace("\"", "\"\"") ?? ""}\"";
                var ownerPhone = $"\"{unit.Owner?.Phone?.Replace("\"", "\"\"") ?? ""}\"";
                var ownerEmail = $"\"{unit.Owner?.Email?.Replace("\"", "\"\"") ?? ""}\"";
                var isAvailable = unit.IsAvailable ? "Yes" : "No";
                var description = $"\"{unit.Description?.Replace("\"", "\"\"") ?? ""}\"";
                var createdBy = $"\"{unit.CreatedBy?.FullName?.Replace("\"", "\"\"") ?? ""}\"";
                var createdAt = unit.CreatedAt.ToString("dd MMM yyyy HH:mm");

                builder.AppendLine($"{unitCode},{projectName},{location},{type},{saleType},{price},{currency},{area},{bedrooms},{bathrooms},{ownerName},{ownerPhone},{ownerEmail},{isAvailable},{description},{createdBy},{createdAt}");
            }

            return Encoding.UTF8.GetBytes(builder.ToString());
        }
    }
}

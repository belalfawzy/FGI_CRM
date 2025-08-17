using FGI.Interfaces;
using FGI.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

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
            return unit;
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
            return await _context.Units.FindAsync(unitId);
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

    }
}

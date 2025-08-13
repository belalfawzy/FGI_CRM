using FGI.Interfaces;
using FGI.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FGI.Services
{
    public class UnitService : IUnitService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UnitService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<Unit>> GetUnitsByProjectIdAsync(int projectId)
        {
            return await _context.Units
                .Where(u => u.ProjectId == projectId)
                .OrderBy(u => u.UnitCode) 
                .ToListAsync();
        }
        public async Task<bool> UnitCodeExists(string unitCode, int projectId)
        {
            return await _context.Units
                .AnyAsync(u => u.UnitCode == unitCode && u.ProjectId == projectId);
        }
        public async Task<Unit> AddUnitAsync(Unit unit)
        {
            // الحصول على معرف المستخدم الحالي
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (await UnitCodeExists(unit.UnitCode, unit.ProjectId ?? 0))
            {
                throw new InvalidOperationException("Unit code already exists for this project.");
            }

            // تعيين المستخدم الذي أضاف الوحدة
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
                .ToListAsync();
        }
        public async Task UpdateUnitAsync(Unit unit)
        {
            _context.Units.Update(unit);
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Unit>> GetAvailableUnitsAsync()
        {
            return await _context.Units
                .Where(u => u.IsAvailable)
                .ToListAsync();
        }

    }
}

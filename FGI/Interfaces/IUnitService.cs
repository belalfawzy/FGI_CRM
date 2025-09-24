using FGI.Models;
using FGI.Enums;

namespace FGI.Interfaces
{
    public interface IUnitService
    {
        Task<List<Unit>> GetUnitsByProjectIdAsync(int projectId);
        Task<Unit> AddUnitAsync(Unit unit);
        Task UpdateAvailabilityAsync(int unitId, bool isAvailable);
        Task DeleteUnitAsync(int unitId);
        Task<Unit> GetUnitByIdAsync(int unitId);
        Task UpdateUnitAsync(Unit unit);
        Task<List<Unit>> GetUnitsByCreatorAsync(int creatorId);
        Task<IEnumerable<Unit>> GetAvailableUnitsAsync();
        Task<object> GetUnitsForSelectAsync(int projectId, string term);
        Task<List<Unit>> GetFilteredUnitsAsync(UnitType? type, int? projectId, decimal? minPrice, decimal? maxPrice, int? bedrooms, bool? isAvailable, decimal? minArea, int? bathrooms, UnitSaleType? saleType, string searchTerm);
    }
}

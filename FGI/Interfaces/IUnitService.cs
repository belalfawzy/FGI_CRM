using FGI.Models;

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

    }
}

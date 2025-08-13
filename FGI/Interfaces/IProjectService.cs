using FGI.Models;

namespace FGI.Interfaces
{
    public interface IProjectService
    {
        Task<List<Project>> GetAllProjectsAsync();
        Task<Project> GetProjectByIdAsync(int id);
        Task<Project> AddProjectAsync(Project project);
        Task DeleteProjectAsync(int projectId);
    }
}

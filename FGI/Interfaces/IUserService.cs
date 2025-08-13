using FGI.Enums;
using FGI.Models;

namespace FGI.Interfaces
{
    public interface IUserService
    {
        Task<User> AuthenticateAsync(string email, string password);
        Task<User> GetByIdAsync(int id);
        Task<List<User>> GetUsersByRoleAsync(UserRole role);
        Task<User> RegisterAsync(User user);
        Task<List<User>> GetAllUsersAsync();
    }
}

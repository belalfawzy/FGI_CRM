using FGI.Enums;
using FGI.Interfaces;
using FGI.Models;
using Microsoft.EntityFrameworkCore;

namespace FGI.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }
        public async Task<List<User>> GetUsersByRoleAsync(string role)
        {
            return await _context.Users
       .Where(u => u.Role.ToLower() == role.ToLower()) // تحقق غير حساس لحالة الأحرف
       .ToListAsync();
        }

        public async Task<User> AuthenticateAsync(string email, string password)
        {
            var hashed = password;
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == hashed);
        }

        public async Task<User> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<List<User>> GetUsersByRoleAsync(UserRole role)
        {
            return await _context.Users
                .Where(u => u.Role == role.ToString())
                .ToListAsync();
        }

        public async Task<User> RegisterAsync(User user)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
                if (existingUser != null)
                {
                    return null; // Email already exists
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return user;
            }
            catch (Exception)
            {
                return null; // Error occurred
            }
        }
    }
}

using BCrypt.Net;
using LedgerProject.Data;
using LedgerProject.Models;
using Microsoft.EntityFrameworkCore;

namespace LedgerProject.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateUserAsync(string username, string password, List<string> roles)
        {
            if (await _context.Users.AnyAsync(u => u.Username == username))
                throw new Exception("User already exists.");

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true
            };

            _context.Users.Add(user);

            foreach (var roleName in roles)
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);

                if (role == null)
                    throw new Exception($"Role {roleName} does not exist.");

                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.UserId,
                    RoleId = role.RoleId
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<User> ValidateUserAsync(string username, string password)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null)
                return null;

            var validPassword = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            return validPassword ? user : null;
        }
    }
}
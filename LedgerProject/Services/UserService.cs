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

            if (roles.Contains("User"))
            {
                var account = new Account
                {
                    AccountId = Guid.NewGuid(),
                    AccountName = username,
                    AccountType = "Person",
                    Status = LedgerProject.Models.Enums.AccountStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Accounts.Add(account);

                _context.AccountBalances.Add(new AccountBalance
                {
                    AccountId = account.AccountId,
                    Balance = 0,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<object>> GetUsersAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList()
                })
                .Cast<object>()
                .ToListAsync();
        }

        public async Task DeleteUserAsync(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                throw new Exception("User not found.");

            user.IsActive = false;

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountName == username);
            if (account != null)
            {
                account.Status = LedgerProject.Models.Enums.AccountStatus.FullyFrozen;
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
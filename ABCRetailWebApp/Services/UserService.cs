using ABCRetailWebApp.Data;
using ABCRetailWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailWebApp.Services
{
    public class UserService : IUserService
    {
        private readonly ABCRetailDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ABCRetailDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserEntity?> AuthenticateUserAsync(string email, string password)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Email == email && u.Password == password && u.IsActive)
                    .FirstOrDefaultAsync();

                if (user != null)
                {
                    await UpdateLastLoginAsync(user.UserId);
                    _logger.LogInformation("User authenticated successfully: {Email}", email);
                    return user;
                }

                _logger.LogWarning("Authentication failed for email: {Email}", email);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating user: {Email}", email);
                throw;
            }
        }

        public async Task<UserEntity?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.CartItems)
                    .Include(u => u.Orders)
                    .FirstOrDefaultAsync(u => u.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<UserEntity?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                throw;
            }
        }

        public async Task<IEnumerable<UserEntity>> GetAllUsersAsync()
        {
            try
            {
                return await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Username)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }

        public async Task<UserEntity> CreateUserAsync(UserEntity user)
        {
            try
            {
                var existingUser = await GetUserByEmailAsync(user.Email);
                if (existingUser != null)
                {
                    throw new InvalidOperationException("User with this email already exists.");
                }

                user.CreatedDate = DateTime.UtcNow;
                user.IsActive = true;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User created successfully: {Email}", user.Email);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<bool> UpdateUserAsync(UserEntity user)
        {
            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User updated successfully: UserId={UserId}", user.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", user.UserId);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for deletion: UserId={UserId}", userId);
                    return false;
                }

                user.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("User soft deleted: UserId={UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", userId);
                return false;
            }
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.LastLoginDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating last login for UserId: {UserId}", userId);
            }
        }
    }
}

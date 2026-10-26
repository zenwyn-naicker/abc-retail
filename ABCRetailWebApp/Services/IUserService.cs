using ABCRetailWebApp.Models;

namespace ABCRetailWebApp.Services
{
    public interface IUserService
    {
        Task<UserEntity?> AuthenticateUserAsync(string email, string password);
        Task<UserEntity?> GetUserByIdAsync(int userId);
        Task<UserEntity?> GetUserByEmailAsync(string email);
        Task<IEnumerable<UserEntity>> GetAllUsersAsync();
        Task<UserEntity> CreateUserAsync(UserEntity user);
        Task<bool> UpdateUserAsync(UserEntity user);
        Task<bool> DeleteUserAsync(int userId);
        Task UpdateLastLoginAsync(int userId);
    }
}

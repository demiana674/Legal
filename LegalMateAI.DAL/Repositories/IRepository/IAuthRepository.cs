using LegalMateAI.Domain.Entities;

namespace LegalMateAI.DAL.Repositories.IRepository
{
    public interface IAuthRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<Admin?> GetAdminByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task AddUserAsync(User user);
        Task AddAdminAsync(Admin admin);
        Task LogLoginAttemptAsync(Guid? userId, string email, bool isSuccess);
        Task<bool> EmailExistsAsync(string email);
        Task SaveChangesAsync();
        void AddLoginAttempt(LoginAttempt attempt);
    }
}
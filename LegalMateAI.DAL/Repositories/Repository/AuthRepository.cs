using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.DAL.Repositories.IRepository;

namespace LegalMateAI.DAL.Repositories.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly LegalMateDbContext _context;

        public AuthRepository(LegalMateDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Admin?> GetAdminByEmailAsync(string email)
        {
            return await _context.Admins
                .FirstOrDefaultAsync(a => a.Email == email);
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId);
        }

        public async Task AddUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task AddAdminAsync(Admin admin)
        {
            await _context.Admins.AddAsync(admin);
        }

        public async Task LogLoginAttemptAsync(Guid? userId, Guid? adminId, string email, bool isSuccess)
        {
            var attempt = new LoginAttempt
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AdminId = adminId,
                Email = email,
                IsSuccess = isSuccess,
                AttemptedAt = DateTime.UtcNow
            };
            await _context.LoginAttempts.AddAsync(attempt);
        }

        public void AddLoginAttempt(LoginAttempt attempt)
        {
            _context.LoginAttempts.Add(attempt);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        
        public async Task AddSessionAsync(Session session)
        {
            await _context.Sessions.AddAsync(session);
        }
        
        public async Task UpdateSessionAsync(Session session)
        {
            _context.Sessions.Update(session);
            await Task.CompletedTask;
        }
        
        public async Task<Session?> GetSessionByTokenAsync(string token)
        {
            return await _context.Sessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionToken == token && s.IsActive && s.ExpiresAt > DateTime.UtcNow);
        }
    }
}
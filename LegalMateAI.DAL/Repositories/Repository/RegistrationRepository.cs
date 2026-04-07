// LegalMateAI.DAL/Repositories/Repository/RegistrationRepository.cs
using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.DAL.Repositories.IRepository;

namespace LegalMateAI.DAL.Repositories.Repository
{
    public class RegistrationRepository : IRegistrationRepository
    {
        private readonly LegalMateDbContext _context;

        public RegistrationRepository(LegalMateDbContext context)
        {
            _context = context;
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        // ✅ التحقق من تكرار الرقم القومي
        public async Task<bool> NationalIdExistsAsync(string nationalId)
        {
            return await _context.Users.AnyAsync(u => u.NationalId == nationalId);
        }

        // ✅ التحقق من تكرار رقم رخصة المحاماة
        public async Task<bool> LicenseNumberExistsAsync(string licenseNumber)
        {
            return await _context.LawyerProfiles
                .AnyAsync(l => l.LicenseNumber == licenseNumber);
        }

        public async Task AddUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task AddUserProfileAsync(UserProfile profile)
        {
            await _context.UserProfiles.AddAsync(profile);
        }

        public async Task AddUserWithProfileAsync(User user, UserProfile profile)
        {
            await _context.Users.AddAsync(user);
            await _context.UserProfiles.AddAsync(profile);
        }

        public async Task AddLawyerWithProfileAsync(User user, LawyerProfile lawyerProfile)
        {
            await _context.Users.AddAsync(user);
            await _context.LawyerProfiles.AddAsync(lawyerProfile);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
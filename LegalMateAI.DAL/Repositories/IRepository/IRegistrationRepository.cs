// LegalMateAI.DAL/Repositories/IRepository/IRegistrationRepository.cs
using LegalMateAI.Domain.Entities;

namespace LegalMateAI.DAL.Repositories.IRepository
{
    public interface IRegistrationRepository
    {
        Task<bool> UserExistsAsync(string email);
        Task<bool> NationalIdExistsAsync(string nationalId);
        Task<bool> LicenseNumberExistsAsync(string licenseNumber);
        Task AddUserAsync(User user);
        Task AddUserProfileAsync(UserProfile profile);
        Task AddUserWithProfileAsync(User user, UserProfile profile);
        Task AddLawyerWithProfileAsync(User user, LawyerProfile lawyerProfile);
        Task SaveChangesAsync();
    }
}
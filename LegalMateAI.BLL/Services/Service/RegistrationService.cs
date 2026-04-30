using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DAL.Repositories.IRepository;
using LegalMateAI.DTOs;
using Microsoft.Extensions.Logging;
using BCrypt.Net;
using LegalMateAI.Infrastructure.Services.IService;

namespace LegalMateAI.BLL.Services.Service
{
    public class RegistrationService : IRegistrationService
    {
        private readonly IRegistrationRepository _repo;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<RegistrationService> _logger;

        public RegistrationService(
            IRegistrationRepository repo,
            IEncryptionService encryption,
            ILogger<RegistrationService> logger)
        {
            _repo = repo;
            _encryption = encryption;
            _logger = logger;
        }

        public async Task<RegistrationResult> RegisterAsync(RegisterRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                    return new RegistrationResult { Success = false, Message = "البريد الإلكتروني وكلمة المرور مطلوبان" };

                if (request.Password != request.ConfirmPassword)
                    return new RegistrationResult { Success = false, Message = "كلمة المرور وتأكيدها غير متطابقين" };

                if (await _repo.UserExistsAsync(request.Email))
                    return new RegistrationResult { Success = false, Message = "البريد الإلكتروني موجود بالفعل" };

                if (await _repo.NationalIdExistsAsync(_encryption.Encrypt(request.NationalId)))
                    return new RegistrationResult { Success = false, Message = "الرقم القومي موجود بالفعل" };

                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                var user = new User
                {
                    UserID = Guid.NewGuid(),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    Phone = _encryption.Encrypt(request.Phone),
                    NationalId = _encryption.Encrypt(request.NationalId),
                    Nationality = request.Nationality,
                    DateOfBirth = request.DateOfBirth,
                    Role = request.Role,
                    IsActive = request.Role == UserRole.User, // المستخدم العادي يتفعّل فوراً
                    Status = AccountStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    JoinDate = DateTime.UtcNow,
                    EmailVerified = false
                };

                if (request.Role == UserRole.User)
                {
                    var userProfile = new UserProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.UserID,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        Email = request.Email,
                        PhoneNumber = _encryption.Encrypt(request.Phone),
                        NationalId = _encryption.Encrypt(request.NationalId),
                        Nationality = request.Nationality,
                        DateOfBirth = request.DateOfBirth,
                        GovernorateId = request.GovernorateId,
                        CityId = request.CityId,
                        Address = request.Address,
                        CreatedAt = DateTime.UtcNow,
                        LastProfileUpdate = DateTime.UtcNow
                    };

                    await _repo.AddUserWithProfileAsync(user, userProfile);
                    await _repo.SaveChangesAsync();

                    return new RegistrationResult
                    {
                        Success = true,
                        Message = "تم تسجيل المستخدم بنجاح",
                        UserId = user.UserID,
                        RequiresApproval = false
                    };
                }
                else if (request.Role == UserRole.Lawyer)
                {
                    if (string.IsNullOrEmpty(request.LicenseNumber))
                        return new RegistrationResult { Success = false, Message = "رقم رخصة المحاماة مطلوب" };

                    if (await _repo.LicenseNumberExistsAsync(_encryption.Encrypt(request.LicenseNumber)))
                        return new RegistrationResult { Success = false, Message = "رقم رخصة المحاماة موجود بالفعل" };

                    // المحامي لا يتفعّل إلا بعد موافقة الأدمن
                    user.IsActive = false;
                    user.Status = AccountStatus.Pending;

                    var lawyerProfile = new LawyerProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.UserID,
                        LicenseNumber = _encryption.Encrypt(request.LicenseNumber),
                        BarAssociation = request.BarAssociation ?? "",
                        LicenseIssueDate = request.LicenseIssueDate,
                        PracticeDegree = request.PracticeDegree,
                        YearsOfExperience = request.YearsOfExperience ?? 0,
                        PhoneNumber = _encryption.Encrypt(request.Phone),
                        GovernorateId = request.GovernorateId,
                        CityId = request.CityId,
                        OfficeAddress = request.Address,
                        VerificationStatus = LawyerVerificationStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _repo.AddLawyerWithProfileAsync(user, lawyerProfile);
                    await _repo.SaveChangesAsync();

                    return new RegistrationResult
                    {
                        Success = true,
                        Message = "تم تقديم طلب تسجيل المحامي بنجاح، في انتظار موافقة الإدارة",
                        UserId = user.UserID,
                        RequiresApproval = true
                    };
                }

                return new RegistrationResult { Success = false, Message = "نوع المستخدم غير صالح" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration error for email: {Email}", request.Email);
                return new RegistrationResult { Success = false, Message = "حدث خطأ أثناء التسجيل" };
            }
        }

        public async Task<RegistrationResult> RegisterUserAsync(RegisterRequest request)
        {
            request.Role = UserRole.User;
            return await RegisterAsync(request);
        }
    }
}
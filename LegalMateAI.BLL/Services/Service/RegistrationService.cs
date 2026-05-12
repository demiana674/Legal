// LegalMateAI.BLL/Services/Service/RegistrationService.cs
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
                {
                    return new RegistrationResult 
                    { 
                        Success = false, 
                        Message = "البريد الإلكتروني وكلمة المرور مطلوبان" 
                    };
                }

                if (request.Password != request.ConfirmPassword)
                {
                    return new RegistrationResult 
                    { 
                        Success = false, 
                        Message = "كلمة المرور وتأكيدها غير متطابقين" 
                    };
                }

                if (!IsStrongPassword(request.Password))
                {
                    return new RegistrationResult 
                    { 
                        Success = false, 
                        Message = "كلمة المرور يجب أن تحتوي على: 8 أحرف على الأقل، حرف كبير، حرف صغير، رقم، ورمز خاص (@$!%*?&)" 
                    };
                }

                if (await _repo.UserExistsAsync(request.Email))
                {
                    _logger.LogWarning("Registration failed - email exists: {Email}", request.Email);
                    return new RegistrationResult 
                    { 
                        Success = false, 
                        Message = "البريد الإلكتروني موجود بالفعل" 
                    };
                }

                if (await _repo.NationalIdExistsAsync(request.NationalId))
                {
                    _logger.LogWarning("Registration failed - national ID exists: {NationalId}", request.NationalId);
                    return new RegistrationResult 
                    { 
                        Success = false, 
                        Message = "الرقم القومي موجود بالفعل" 
                    };
                }

                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                var encryptedPhone = string.IsNullOrEmpty(request.Phone) ? null : _encryption.Encrypt(request.Phone);
                var encryptedNationalId = _encryption.Encrypt(request.NationalId);
                
                var user = new User
                {
                    UserID = Guid.NewGuid(),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    Phone = encryptedPhone,
                    NationalId = encryptedNationalId,
                    Nationality = request.Nationality,
                    DateOfBirth = request.DateOfBirth,
                    Role = request.Role,
                    IsActive = false,  // ✅ كل المستخدمين يبدأوا غير نشطين
                    Status = AccountStatus.Pending,  // ✅ الحالة الافتراضية: Pending
                    CreatedAt = DateTime.UtcNow,
                    JoinDate = DateTime.UtcNow,
                    EmailVerified = false
                };

                // ✅ تسجيل مستخدم عادي - يتفعل فوراً
                if (request.Role == UserRole.User)
                {
                    user.IsActive = true;  // المستخدم العادي يتفعل مباشرة
                    user.Status = AccountStatus.Active;
                    
                    var userProfile = new UserProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.UserID,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        Email = request.Email,
                        PhoneNumber = request.Phone,
                        NationalId = request.NationalId,
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
                    
                    _logger.LogInformation("User registered successfully: {Email}", request.Email);
                    
                    return new RegistrationResult
                    {
                        Success = true,
                        Message = "تم تسجيل المستخدم بنجاح",
                        UserId = user.UserID,
                        RequiresApproval = false
                    };
                }
                
                // ✅ تسجيل محامي - يحتاج موافقة الأدمن
                else if (request.Role == UserRole.Lawyer)
                {
                    if (string.IsNullOrEmpty(request.LicenseNumber))
                    {
                        return new RegistrationResult
                        {
                            Success = false,
                            Message = "رقم رخصة المحاماة مطلوب للمحامي"
                        };
                    }

                    if (!IsValidLicenseNumber(request.LicenseNumber))
                    {
                        return new RegistrationResult
                        {
                            Success = false,
                            Message = "صيغة رخصة المحاماة غير صحيحة. يجب أن تكون مثل: LAW-12345 أو BAR-67890"
                        };
                    }

                    if (await _repo.LicenseNumberExistsAsync(request.LicenseNumber))
                    {
                        _logger.LogWarning("Registration failed - license number exists: {LicenseNumber}", request.LicenseNumber);
                        return new RegistrationResult
                        {
                            Success = false,
                            Message = "رقم رخصة المحاماة موجود بالفعل"
                        };
                    }

                    // ✅ المحامي يبدأ غير نشط وفي حالة Pending
                    user.IsActive = false;
                    user.Status = AccountStatus.Pending;
                    
                    var lawyerProfile = new LawyerProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.UserID,
                        LicenseNumber = request.LicenseNumber,
                        BarAssociation = request.BarAssociation ?? "",
                        LicenseIssueDate = request.LicenseIssueDate,
                        PracticeDegree = request.PracticeDegree,
                        YearsOfExperience = request.YearsOfExperience ?? 0,
                        GovernorateId = request.GovernorateId,
                        CityId = request.CityId,
                        OfficeAddress = request.Address,
                        VerificationStatus = LawyerVerificationStatus.Pending,  // ✅ أهم سطر: حالة Pending
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await _repo.AddLawyerWithProfileAsync(user, lawyerProfile);
                    await _repo.SaveChangesAsync();
                    
                    _logger.LogInformation("Lawyer registration submitted: {Email}, License: {LicenseNumber}", 
                        request.Email, request.LicenseNumber);
                    
                    return new RegistrationResult
                    {
                        Success = true,
                        Message = "تم تقديم طلب تسجيل المحامي بنجاح، في انتظار موافقة الإدارة",
                        UserId = user.UserID,
                        RequiresApproval = true  // ✅ المحامي يحتاج موافقة
                    };
                }

                return new RegistrationResult
                {
                    Success = false,
                    Message = "نوع المستخدم غير صالح"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration error for email: {Email}", request.Email);
                return new RegistrationResult
                {
                    Success = false,
                    Message = "حدث خطأ أثناء التسجيل. يرجى المحاولة مرة أخرى."
                };
            }
        }

        public async Task<RegistrationResult> RegisterUserAsync(RegisterRequest request)
        {
            request.Role = UserRole.User;
            return await RegisterAsync(request);
        }

        private bool IsStrongPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;
            
            bool hasUpper = false;
            bool hasLower = false;
            bool hasDigit = false;
            bool hasSpecial = false;
            
            foreach (char c in password)
            {
                if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsLower(c)) hasLower = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else if ("@$!%*?&".Contains(c)) hasSpecial = true;
            }
            
            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        private bool IsValidLicenseNumber(string licenseNumber)
        {
            if (string.IsNullOrEmpty(licenseNumber))
                return false;
            
            var regex = new System.Text.RegularExpressions.Regex(@"^[A-Z]{2,3}-\d{4,8}$");
            return regex.IsMatch(licenseNumber);
        }
    }
}
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DAL.Repositories.IRepository;
using LegalMateAI.DTOs;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace LegalMateAI.BLL.Services.Service
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepo;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IAuthRepository authRepo,
            IJwtService jwtService,
            ILogger<AuthService> logger)
        {
            _authRepo = authRepo;
            _jwtService = jwtService;
            _logger = logger;
        }

        /// <summary>
        /// تسجيل الدخول الموحد لجميع المستخدمين (User, Lawyer, Admin)
        /// </summary>
        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    _logger.LogWarning("Login attempt with empty credentials");
                    return null;
                }

                // 1. البحث عن مستخدم عادي أو محامي
                var user = await _authRepo.GetUserByEmailAsync(request.Email);
                if (user != null)
                {
                    return await HandleUserLogin(user, request);
                }

                // 2. البحث عن أدمن
                var admin = await _authRepo.GetAdminByEmailAsync(request.Email);
                if (admin != null)
                {
                    return await HandleAdminLogin(admin, request);
                }

                // 3. لا يوجد مستخدم بهذا البريد
                await _authRepo.LogLoginAttemptAsync(null, request.Email, false);
                _logger.LogWarning("Login failed - user not found: {Email}", request.Email);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for email: {Email}", request.Email);
                throw;
            }
        }

        /// <summary>
        /// معالجة تسجيل دخول المستخدم العادي أو المحامي
        /// </summary>
        private async Task<AuthResponse?> HandleUserLogin(User user, LoginRequest request)
        {
            // التحقق من كلمة المرور
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                await _authRepo.LogLoginAttemptAsync(user.UserID, request.Email, false);
                _logger.LogWarning("Login failed - invalid password: {Email}", request.Email);
                return null;
            }

            // ✅ التحقق من حالة الحساب باستخدام AccountStatus (داخلياً فقط)
            switch (user.Status)
            {
                case AccountStatus.Suspended:
                    await _authRepo.LogLoginAttemptAsync(user.UserID, request.Email, false);
                    _logger.LogWarning("Login failed - suspended account: {Email}", request.Email);
                    throw new UnauthorizedAccessException("الحساب معلق. يرجى التواصل مع الدعم.");

                case AccountStatus.Locked:
                    await _authRepo.LogLoginAttemptAsync(user.UserID, request.Email, false);
                    _logger.LogWarning("Login failed - locked account: {Email}", request.Email);
                    throw new UnauthorizedAccessException("الحساب مقفل. يرجى إعادة تعيين كلمة المرور.");

                case AccountStatus.Deactivated:
                    await _authRepo.LogLoginAttemptAsync(user.UserID, request.Email, false);
                    _logger.LogWarning("Login failed - deactivated account: {Email}", request.Email);
                    throw new UnauthorizedAccessException("الحساب معطل. يرجى التواصل مع الدعم.");

                case AccountStatus.Pending:
                    if (user.Role == UserRole.Lawyer)
                    {
                        await _authRepo.LogLoginAttemptAsync(user.UserID, request.Email, false);
                        _logger.LogWarning("Login failed - pending lawyer: {Email}", request.Email);
                        throw new UnauthorizedAccessException("حساب المحامي لم يتم توثيقه بعد. يرجى الانتظار حتى مراجعة الإدارة.");
                    }
                    break;

                case AccountStatus.Active:
                    // الحساب نشط، نسمح بالدخول
                    break;

                default:
                    await _authRepo.LogLoginAttemptAsync(user.UserID, request.Email, false);
                    _logger.LogWarning("Login failed - unknown status: {Status} for user: {Email}", user.Status, request.Email);
                    throw new UnauthorizedAccessException("حالة الحساب غير معروفة. يرجى التواصل مع الدعم.");
            }

            // تحديث آخر تسجيل دخول
            user.LastLogin = DateTime.UtcNow;
            
            // توليد JWT Token
            var token = _jwtService.GenerateToken(user);
            
            // تسجيل محاولة ناجحة
            await _authRepo.LogLoginAttemptAsync(user.UserID, request.Email, true);
            await _authRepo.SaveChangesAsync();
            
            _logger.LogInformation("User logged in: {Email}, Role: {Role}", user.Email, user.Role);
            
            // ✅ الـ Response زي ما هو - من غير تغيير
            return new AuthResponse
            {
                UserId = user.UserID,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString(),
                Token = token,
                IsAdmin = false
            };
        }

        /// <summary>
        /// معالجة تسجيل دخول الأدمن
        /// </summary>
        private async Task<AuthResponse?> HandleAdminLogin(Admin admin, LoginRequest request)
        {
            // التحقق من كلمة المرور
            if (!BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
            {
                await _authRepo.LogLoginAttemptAsync(null, request.Email, false);
                _logger.LogWarning("Admin login failed - invalid password: {Email}", request.Email);
                return null;
            }

            // تحديث آخر تسجيل دخول
            admin.LastLoginAt = DateTime.UtcNow;
            
            // توليد JWT Token
            var token = _jwtService.GenerateAdminToken(admin);
            
            // تسجيل محاولة ناجحة
            await _authRepo.LogLoginAttemptAsync(null, request.Email, true);
            await _authRepo.SaveChangesAsync();
            
            _logger.LogInformation("Admin logged in: {Email}", admin.Email);
            
            // ✅ الـ Response زي ما هو - من غير تغيير
            return new AuthResponse
            {
                UserId = admin.Id,
                Email = admin.Email,
                FirstName = admin.FullName.Split(' ').FirstOrDefault() ?? "",
                LastName = admin.FullName.Split(' ').Skip(1).FirstOrDefault() ?? "",
                Role = "Admin",
                Token = token,
                IsAdmin = true
            };
        }

        /// <summary>
        /// تغيير كلمة المرور
        /// </summary>
        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto request)
        {
            try
            {
                var user = await _authRepo.GetUserByIdAsync(userId);
                if (user == null) return false;
                
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                    return false;
                
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _authRepo.SaveChangesAsync();
                
                _logger.LogInformation("Password changed for user: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// تسجيل دخول الأدمن (للتوافق)
        /// </summary>
        public async Task<AuthResponse?> AdminLoginAsync(LoginRequest request)
        {
            return await LoginAsync(request);
        }
    }
}
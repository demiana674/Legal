using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DAL.Repositories.IRepository;
using LegalMateAI.DTOs;
using Microsoft.Extensions.Logging;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;

namespace LegalMateAI.BLL.Services.Service
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepo;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(
            IAuthRepository authRepo,
            IJwtService jwtService,
            ILogger<AuthService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _authRepo = authRepo;
            _jwtService = jwtService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    _logger.LogWarning("Login attempt with empty credentials");
                    return null;
                }

                var user = await _authRepo.GetUserByEmailAsync(request.Email);
                if (user != null)
                {
                    return await HandleUserLogin(user, request);
                }

                var admin = await _authRepo.GetAdminByEmailAsync(request.Email);
                if (admin != null)
                {
                    return await HandleAdminLogin(admin, request);
                }

                await _authRepo.LogLoginAttemptAsync(null, null, request.Email, false);
                _logger.LogWarning("Login failed - user not found: {Email}", request.Email);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for email: {Email}", request.Email);
                throw;
            }
        }

        private async Task<AuthResponse?> HandleUserLogin(User user, LoginRequest request)
        {
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                await _authRepo.LogLoginAttemptAsync(user.UserID, null, request.Email, false);
                _logger.LogWarning("Login failed - invalid password: {Email}", request.Email);
                return null;
            }

            if (!user.IsActive || user.Status == AccountStatus.Deactivated)
            {
                await _authRepo.LogLoginAttemptAsync(user.UserID, null, request.Email, false);
                _logger.LogWarning("Login failed - inactive account: {Email}", request.Email);
                throw new UnauthorizedAccessException("الحساب غير نشط. يرجى التواصل مع الدعم.");
            }

            // المحامي المعلق = ممنوع من الدخول تماماً
            if (user.Role == UserRole.Lawyer && user.LawyerProfile != null)
            {
                if (user.LawyerProfile.VerificationStatus == LawyerVerificationStatus.Suspended)
                {
                    await _authRepo.LogLoginAttemptAsync(user.UserID, null, request.Email, false);
                    _logger.LogWarning("Login blocked - suspended lawyer: {Email}", request.Email);
                    throw new UnauthorizedAccessException("تم تعليق حساب المحامي الخاص بك. يرجى التواصل مع الإدارة.");
                }
                
                if (user.LawyerProfile.VerificationStatus != LawyerVerificationStatus.Active)
                {
                    await _authRepo.LogLoginAttemptAsync(user.UserID, null, request.Email, false);
                    _logger.LogWarning("Login failed - unverified lawyer: {Email}", request.Email);
                    throw new UnauthorizedAccessException("حساب المحامي لم يتم توثيقه بعد. يرجى الانتظار حتى مراجعة الإدارة.");
                }
            }

            // المستخدم المعلق = يدخل كـ Guest فقط
            if (user.Role == UserRole.User && user.Status == AccountStatus.Suspended)
            {
                user.LastLogin = DateTime.UtcNow;
                var token = _jwtService.GenerateToken(user);
                
                await _authRepo.LogLoginAttemptAsync(user.UserID, null, request.Email, true);
                
                // تسجيل Session
                await CreateSessionAsync(user.UserID);
                
                await _authRepo.SaveChangesAsync();
                
                _logger.LogInformation("Suspended user logged in as Guest: {Email}", user.Email);
                
                return new AuthResponse
                {
                    UserId = user.UserID,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = "Guest",
                    Token = token,
                    IsAdmin = false
                };
            }

            user.LastLogin = DateTime.UtcNow;
            var userToken = _jwtService.GenerateToken(user);
            
            await _authRepo.LogLoginAttemptAsync(user.UserID, null, request.Email, true);
            
            // تسجيل Session
            await CreateSessionAsync(user.UserID);
            
            await _authRepo.SaveChangesAsync();
            
            _logger.LogInformation("User logged in: {Email}, Role: {Role}", user.Email, user.Role);
            
            return new AuthResponse
            {
                UserId = user.UserID,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString(),
                Token = userToken,
                IsAdmin = false
            };
        }

        private async Task<AuthResponse?> HandleAdminLogin(Admin admin, LoginRequest request)
        {
            if (!BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
            {
                await _authRepo.LogLoginAttemptAsync(null, admin.Id, request.Email, false);
                _logger.LogWarning("Admin login failed - invalid password: {Email}", request.Email);
                return null;
            }

            admin.LastLoginAt = DateTime.UtcNow;
            var token = _jwtService.GenerateAdminToken(admin);
            
            await _authRepo.LogLoginAttemptAsync(null, admin.Id, request.Email, true);
            await _authRepo.SaveChangesAsync();
            
            _logger.LogInformation("Admin logged in: {Email}", admin.Email);
            
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

        private async Task CreateSessionAsync(Guid userId)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var session = new Session
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionToken = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                LastActivityAt = DateTime.UtcNow,
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                IsActive = true
            };
            await _authRepo.AddSessionAsync(session);
        }

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
    }
}
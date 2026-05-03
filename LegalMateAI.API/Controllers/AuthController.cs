using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs;
using System.Security.Claims;
using LegalMateAI.Domain.Enums;
namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogService _logService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILogService logService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logService = logService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            claim ??= User.FindFirst("id");
            claim ??= User.FindFirst("sub");
            claim ??= User.FindFirst(ClaimTypes.Name);
            
            if (claim == null)
            {
                _logger.LogWarning("No user ID claim found. Available claims: {Claims}", 
                    string.Join(", ", User.Claims.Select(c => c.Type)));
                throw new UnauthorizedAccessException("User not authenticated");
            }
            
            return Guid.Parse(claim.Value);
        }

        private string GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        }

        /// <summary>
        /// تسجيل الدخول إلى النظام (لجميع المستخدمين: مستخدم عادي، محامي، أدمن)
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse<object>.BadRequest("بيانات غير صحيحة", errors));
            }

            try
            {
                var result = await _authService.LoginAsync(request);
                
                if (result == null)
                {
                    return Unauthorized(ApiResponse<object>.Unauthorized("البريد الإلكتروني أو كلمة المرور غير صحيحة"));
                }

                // ✅ تسجيل عملية تسجيل الدخول
                await _logService.LogActionAsync(result.UserId, AdminLogAction.Login, result.Role, result.UserId);
                _logger.LogInformation("User logged in: {Email}, Role: {Role}", request.Email, result.Role);

                return Ok(ApiResponse<AuthResponse>.Ok(result, "تم تسجيل الدخول بنجاح"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<object>.Unauthorized(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for email: {Email}", request.Email);
                return StatusCode(500, ApiResponse<object>.Fail("حدث خطأ أثناء تسجيل الدخول"));
            }
        }

        /// <summary>
        /// تغيير كلمة المرور
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse<object>.BadRequest("بيانات غير صحيحة", errors));
            }

            try
            {
                var userId = GetUserId();
                var result = await _authService.ChangePasswordAsync(userId, request);
                
                if (!result)
                {
                    return BadRequest(ApiResponse<object>.BadRequest("كلمة المرور الحالية غير صحيحة"));
                }

                // ✅ تسجيل تغيير كلمة المرور
                var role = GetUserRole();
                await _logService.LogActionAsync(userId, AdminLogAction.ChangePassword, role, userId);

                return Ok(ApiResponse<object>.Ok("تم تغيير كلمة المرور بنجاح"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<object>.Unauthorized(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, ApiResponse<object>.Fail("حدث خطأ أثناء تغيير كلمة المرور"));
            }
        }
    }
}
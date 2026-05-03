// LegalMateAI.API/Controllers/RegistrationController.cs
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.Infrastructure.Services.IService;
using LegalMateAI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistrationController : ControllerBase
    {
        private readonly IRegistrationService _regService;
        private readonly ILogService _logService;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<RegistrationController> _logger;

        public RegistrationController(
            IRegistrationService regService,
            ILogService logService,
            IEncryptionService encryption,
            ILogger<RegistrationController> logger)
        {
            _regService = regService;
            _logService = logService;
            _encryption = encryption;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    .ToList();
                return BadRequest(new { message = "بيانات غير صحيحة", errors });
            }

            try
            {
                var result = await _regService.RegisterAsync(request);

                if (!result.Success)
                {
                    _logger.LogWarning("Registration failed: {Message}", result.Message);
                    return BadRequest(new { message = result.Message });
                }

                // ✅ تسجيل عملية التسجيل (للمستخدمين والمحامين)
                var action = request.Role == UserRole.Lawyer ? AdminLogAction.Create : AdminLogAction.Create;
                if (result.UserId.HasValue)
{
    await _logService.LogActionAsync(result.UserId.Value, action, request.Role.ToString(), result.UserId.Value);
}
                _logger.LogInformation("Registration successful for user: {UserId}, Role: {Role}", result.UserId, request.Role);
                
                return Ok(new
                {
                    message = result.Message,
                    requiresApproval = result.RequiresApproval,
                    userId = result.UserId
                });
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
            {
                _logger.LogWarning(ex, "Duplicate key error during registration");
                
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                
                if (errorMessage.Contains("NationalId"))
                {
                    return BadRequest(new { message = "الرقم القومي موجود بالفعل" });
                }
                else if (errorMessage.Contains("Email"))
                {
                    return BadRequest(new { message = "البريد الإلكتروني موجود بالفعل" });
                }
                else if (errorMessage.Contains("LicenseNumber"))
                {
                    return BadRequest(new { message = "رقم رخصة المحاماة موجود بالفعل" });
                }
                
                return BadRequest(new { message = "بيانات مكررة في النظام" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration");
                return StatusCode(500, new { message = "حدث خطأ داخلي في الخادم. يرجى المحاولة مرة أخرى." });
            }
        }
    }
}
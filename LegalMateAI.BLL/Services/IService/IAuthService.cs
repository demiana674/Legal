using LegalMateAI.DTOs;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto request);
    }
}
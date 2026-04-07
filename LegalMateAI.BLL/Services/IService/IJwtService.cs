// LegalMateAI.BLL.Services.IService/IJwtService.cs
using LegalMateAI.Domain.Entities;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        string GenerateAdminToken(Admin admin);
    }
}
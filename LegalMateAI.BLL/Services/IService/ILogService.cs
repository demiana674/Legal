using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.IService
{
    public interface ILogService
    {
        Task LogActionAsync(Guid userId, AdminLogAction action, string targetType, Guid? targetId = null);
    }
}
// LegalMateAI.BLL/Services/IService/INotificationService.cs
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.BLL.Services.IService
{
    public interface INotificationService
    {
        Task<NotificationResponseDto?> SendNotificationAsync(Guid userId, string title, string content, NotificationType type, string? actionUrl = null);
        Task<List<NotificationResponseDto>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false);
        Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId);
        Task<bool> MarkAllAsReadAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
    }
}
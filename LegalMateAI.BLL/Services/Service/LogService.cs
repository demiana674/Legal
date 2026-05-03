using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.BLL.Services.IService;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.BLL.Services.Service
{
    public class LogService : ILogService
    {
        private readonly LegalMateDbContext _context;
        private readonly ILogger<LogService> _logger;

        public LogService(LegalMateDbContext context, ILogger<LogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogActionAsync(Guid userId, AdminLogAction action, string targetType, Guid? targetId = null)
        {
            try
            {
                var log = new AdminLog
                {
                    Id = Guid.NewGuid(),
                    ActorId = userId,
                    Action = action,
                    TargetType = targetType,
                    TargetId = targetId ?? Guid.Empty,
                    Timestamp = DateTime.UtcNow
                };
                _context.AdminLogs.Add(log);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ Log saved: User={UserId}, Action={Action}, Target={TargetType}", 
                    userId, action, targetType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to save log for user {UserId}", userId);
            }
        }
    }
}
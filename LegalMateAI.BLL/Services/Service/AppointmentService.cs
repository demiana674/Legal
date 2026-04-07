using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.BLL.Services.Service
{
    /// <summary>
    /// خدمة إدارة المواعيد
    /// </summary>
    public class AppointmentService : IAppointmentService
    {
        private readonly LegalMateDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AppointmentService> _logger;

        public AppointmentService(
            LegalMateDbContext context,
            INotificationService notificationService,
            ILogger<AppointmentService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// إنشاء موعد جديد
        /// </summary>
        public async Task<AppointmentResponseDto?> CreateAppointmentAsync(Guid userId, CreateAppointmentDto request)
        {
            _logger.LogInformation($"CreateAppointmentAsync START - UserId: {userId}, LawyerId: {request.LawyerId}");
            
            try
            {
                // ✅ التحقق من وجود المستخدم
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {userId}");
                    return null;
                }
                _logger.LogInformation($"User found: {user.Email}");

                // ✅ جلب المحامي مع LawyerProfile
                var lawyerUser = await _context.Users
                    .Include(u => u.LawyerProfile)
                    .FirstOrDefaultAsync(u => u.UserID == request.LawyerId && u.Role == UserRole.Lawyer);
                
                if (lawyerUser?.LawyerProfile == null)
                {
                    _logger.LogWarning($"Lawyer not found: {request.LawyerId}");
                    return null;
                }
                
                var lawyerProfile = lawyerUser.LawyerProfile;
                _logger.LogInformation($"Lawyer found: {lawyerUser.Email}, ProfileId: {lawyerProfile.Id}");

                // ✅ التحقق من توفر الموعد
                var isAvailable = await IsLawyerAvailableAsync(
                    lawyerProfile.Id,
                    request.Date,
                    request.Time,
                    request.DurationMinutes);

                if (!isAvailable)
                {
                    _logger.LogWarning($"Time slot not available for lawyer {request.LawyerId}");
                    return null;
                }

                // ✅ إنشاء الموعد
                var appointment = new Appointment
                {
                    Id = Guid.NewGuid(),
                    AppointmentNumber = GenerateAppointmentNumber(),
                    UserID = userId,
                    LawyerId = lawyerProfile.Id,
                    AppointmentType = request.AppointmentType,
                    Date = request.Date,
                    Time = request.Time,
                    DurationMinutes = request.DurationMinutes,
                    Location = request.Location,
                    Notes = request.Notes,
                    Status = AppointmentStatus.Pending,
                    IsUrgent = request.IsUrgent,
                    RequestedAt = DateTime.UtcNow
                };

                _context.Appointments.Add(appointment);
                var savedRows = await _context.SaveChangesAsync();
                _logger.LogInformation($"Saved rows: {savedRows}, AppointmentId: {appointment.Id}");

                if (savedRows == 0)
                {
                    _logger.LogWarning("No rows saved!");
                    return null;
                }

                // إرسال إشعار للمحامي
                await _notificationService.SendNotificationAsync(
                    lawyerProfile.UserId,
                    "موعد جديد",
                    $"المستخدم {user.FirstName} {user.LastName} حجز موعد {request.AppointmentType} في {request.Date:dd/MM/yyyy} الساعة {request.Time}",
                    NotificationType.Info,
                    $"/appointments/{appointment.Id}"
                );

                return await GetAppointmentByIdAsync(appointment.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateAppointmentAsync");
                throw;
            }
        }

        /// <summary>
        /// الحصول على مواعيد المستخدم
        /// </summary>
        public async Task<List<AppointmentResponseDto>> GetUserAppointmentsAsync(Guid userId, string? status = null)
        {
            var query = _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.Specialties)
                    .ThenInclude(s => s.Specialty)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.Reviews)
                .Where(a => a.UserID == userId)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AppointmentStatus>(status, true, out var statusEnum))
                query = query.Where(a => a.Status == statusEnum);

            var appointments = await query
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.Time)
                .ToListAsync();

            return appointments.Select(a => MapToDto(a)).ToList();
        }

        /// <summary>
        /// الحصول على مواعيد المحامي
        /// </summary>
        public async Task<List<AppointmentResponseDto>> GetLawyerAppointmentsAsync(Guid lawyerId, string? status = null)
        {
            var query = _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.Specialties)
                    .ThenInclude(s => s.Specialty)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.Reviews)
                .Where(a => a.LawyerId == lawyerId)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AppointmentStatus>(status, true, out var statusEnum))
                query = query.Where(a => a.Status == statusEnum);

            var appointments = await query
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.Time)
                .ToListAsync();

            return appointments.Select(a => MapToDto(a)).ToList();
        }

        /// <summary>
        /// الحصول على موعد محدد
        /// </summary>
        public async Task<AppointmentResponseDto?> GetAppointmentByIdAsync(Guid appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.Specialties)
                    .ThenInclude(s => s.Specialty)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.Reviews)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) return null;

            return MapToDto(appointment);
        }

        /// <summary>
        /// إلغاء موعد
        /// </summary>
        public async Task<bool> CancelAppointmentAsync(Guid appointmentId, string? reason = null)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) return false;

            if (appointment.Status == AppointmentStatus.Completed)
                return false;

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancelledAt = DateTime.UtcNow;
            appointment.CancellationReason = reason;

            await _context.SaveChangesAsync();

            // إرسال إشعار للطرف الآخر
            var recipientId = appointment.UserID;
            await _notificationService.SendNotificationAsync(
                recipientId,
                "تم إلغاء موعد",
                $"تم إلغاء الموعد {appointment.AppointmentNumber}",
                NotificationType.Warning,
                $"/appointments/{appointment.Id}"
            );

            _logger.LogInformation("Appointment cancelled: {AppointmentId}", appointmentId);
            return true;
        }

        /// <summary>
        /// طلب إعادة جدولة موعد
        /// </summary>
        public async Task<RescheduleResponseDto?> RequestRescheduleAsync(
            Guid userId, CreateRescheduleRequestDto request, bool isLawyer = false)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Reschedules)
                .FirstOrDefaultAsync(a => a.Id == request.AppointmentId);
            
            if (appointment == null) return null;

            // التحقق من الصلاحية
            if (!isLawyer && appointment.UserID != userId)
                return null;
            
            if (isLawyer && appointment.LawyerId != userId)
                return null;

            // التحقق من حالة الموعد
            if (appointment.Status == AppointmentStatus.Completed || 
                appointment.Status == AppointmentStatus.Cancelled)
                return null;

            // التحقق من توفر الموعد الجديد
            var isAvailable = await IsLawyerAvailableAsync(
                appointment.LawyerId,
                request.NewDate,
                request.NewTime,
                appointment.DurationMinutes,
                appointment.Id);
            
            if (!isAvailable) return null;

            // إنشاء طلب إعادة الجدولة
            var reschedule = new AppointmentReschedule
            {
                Id = Guid.NewGuid(),
                AppointmentId = request.AppointmentId,
                OldDate = appointment.Date,
                OldTime = appointment.Time,
                NewDate = request.NewDate,
                NewTime = request.NewTime,
                InitiatedBy = isLawyer ? RescheduleInitiator.Lawyer : RescheduleInitiator.User,
                Status = RescheduleStatus.Pending,
                Reason = request.Reason,
                RequestedAt = DateTime.UtcNow
            };

            _context.AppointmentReschedules.Add(reschedule);
            appointment.Status = AppointmentStatus.Rescheduled;
            
            await _context.SaveChangesAsync();

            // إرسال إشعار للطرف الآخر
            var recipientId = isLawyer ? appointment.UserID : appointment.LawyerId;
            await _notificationService.SendNotificationAsync(
                recipientId,
                "طلب إعادة جدولة موعد",
                $"تم طلب إعادة جدولة الموعد {appointment.AppointmentNumber} إلى {request.NewDate:dd/MM/yyyy} الساعة {request.NewTime}",
                NotificationType.Warning,
                $"/appointments/reschedule/{reschedule.Id}"
            );

            _logger.LogInformation("Reschedule requested for appointment: {AppointmentId}", request.AppointmentId);
            
            return new RescheduleResponseDto
            {
                Id = reschedule.Id,
                OldDate = reschedule.OldDate,
                OldTime = reschedule.OldTime,
                NewDate = reschedule.NewDate,
                NewTime = reschedule.NewTime,
                InitiatedBy = reschedule.InitiatedBy,
                Status = reschedule.Status,
                Reason = reschedule.Reason,
                RequestedAt = reschedule.RequestedAt
            };
        }

        /// <summary>
        /// الموافقة أو رفض طلب إعادة الجدولة
        /// </summary>
        public async Task<bool> RespondToRescheduleAsync(
            Guid userId, Guid rescheduleId, UpdateRescheduleRequestDto request, bool isLawyer = false)
        {
            var reschedule = await _context.AppointmentReschedules
                .Include(r => r.Appointment)
                .FirstOrDefaultAsync(r => r.Id == rescheduleId);
            
            if (reschedule == null) return false;

            var appointment = reschedule.Appointment;

            // التحقق من الصلاحية
            if (!isLawyer && appointment.LawyerId != userId)
                return false;
            
            if (isLawyer && appointment.UserID != userId)
                return false;

            reschedule.Status = request.Status;
            reschedule.RespondedAt = DateTime.UtcNow;

            if (request.Status == RescheduleStatus.Approved)
            {
                appointment.Date = reschedule.NewDate;
                appointment.Time = reschedule.NewTime;
                appointment.Status = AppointmentStatus.Confirmed;
            }
            else if (request.Status == RescheduleStatus.Rejected)
            {
                appointment.Status = AppointmentStatus.Confirmed;
            }

            await _context.SaveChangesAsync();

            // إرسال إشعار للطرف الآخر
            var recipientId = isLawyer ? appointment.UserID : appointment.LawyerId;
            var resultText = request.Status == RescheduleStatus.Approved ? "تمت الموافقة" : "تم الرفض";
            
            await _notificationService.SendNotificationAsync(
                recipientId,
                request.Status == RescheduleStatus.Approved ? "تم قبول إعادة الجدولة" : "تم رفض إعادة الجدولة",
                $"{resultText} على طلب إعادة جدولة الموعد {appointment.AppointmentNumber}",
                request.Status == RescheduleStatus.Approved ? NotificationType.Success : NotificationType.Error,
                $"/appointments/{appointment.Id}"
            );

            return true;
        }

        /// <summary>
        /// جلب طلبات إعادة الجدولة للمحامي
        /// </summary>
        public async Task<List<RescheduleResponseDto>> GetRescheduleRequestsForLawyerAsync(Guid lawyerId)
        {
            var reschedules = await _context.AppointmentReschedules
                .Include(r => r.Appointment)
                .Where(r => r.Appointment.LawyerId == lawyerId && r.Status == RescheduleStatus.Pending)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            return reschedules.Select(r => new RescheduleResponseDto
            {
                Id = r.Id,
                OldDate = r.OldDate,
                OldTime = r.OldTime,
                NewDate = r.NewDate,
                NewTime = r.NewTime,
                InitiatedBy = r.InitiatedBy,
                Status = r.Status,
                Reason = r.Reason,
                RequestedAt = r.RequestedAt
            }).ToList();
        }

        /// <summary>
        /// جلب طلبات إعادة الجدولة للمستخدم
        /// </summary>
        public async Task<List<RescheduleResponseDto>> GetRescheduleRequestsForUserAsync(Guid userId)
        {
            var reschedules = await _context.AppointmentReschedules
                .Include(r => r.Appointment)
                .Where(r => r.Appointment.UserID == userId && r.Status == RescheduleStatus.Pending)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            return reschedules.Select(r => new RescheduleResponseDto
            {
                Id = r.Id,
                OldDate = r.OldDate,
                OldTime = r.OldTime,
                NewDate = r.NewDate,
                NewTime = r.NewTime,
                InitiatedBy = r.InitiatedBy,
                Status = r.Status,
                Reason = r.Reason,
                RequestedAt = r.RequestedAt
            }).ToList();
        }

        #region Private Methods

        /// <summary>
        /// التحقق من توفر المحامي في الوقت المحدد
        /// </summary>
        private async Task<bool> IsLawyerAvailableAsync(
            Guid lawyerProfileId,
            DateTime date, 
            string time, 
            int duration, 
            Guid? excludeAppointmentId = null)
        {
            _logger.LogInformation($"IsLawyerAvailableAsync - LawyerProfileId: {lawyerProfileId}, Date: {date}, Time: {time}");
            
            var startTime = ParseTime(time);
            var endTime = startTime.Add(TimeSpan.FromMinutes(duration));

            var appointments = await _context.Appointments
                .Where(a => a.LawyerId == lawyerProfileId &&
                            a.Date == date &&
                            a.Status != AppointmentStatus.Cancelled &&
                            a.Status != AppointmentStatus.Completed &&
                            (!excludeAppointmentId.HasValue || a.Id != excludeAppointmentId.Value))
                .ToListAsync();

            _logger.LogInformation($"Found {appointments.Count} existing appointments at this time");

            return !appointments.Any(a =>
            {
                var aStart = ParseTime(a.Time);
                var aEnd = aStart.Add(TimeSpan.FromMinutes(a.DurationMinutes));
                return startTime < aEnd && endTime > aStart;
            });
        }

        /// <summary>
        /// توليد رقم موعد فريد
        /// </summary>
        private string GenerateAppointmentNumber()
        {
            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month;
            var count = _context.Appointments.Count() + 1;
            return $"APT-{year}{month:D2}-{count:D6}";
        }

        /// <summary>
        /// تحويل الوقت من string إلى TimeSpan (يدعم صيغ متعددة)
        /// </summary>
        private TimeSpan ParseTime(string time)
        {
            time = time.Trim();
            
            // صيغة 12 ساعة (مثل "10:00 AM")
            if (time.Contains("AM") || time.Contains("PM"))
            {
                var parts = time.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var timePart = parts[0];
                var period = parts[1];
                
                var timeParts = timePart.Split(':');
                var hour = int.Parse(timeParts[0]);
                var minute = int.Parse(timeParts[1]);
                
                if (period == "PM" && hour != 12)
                    hour += 12;
                else if (period == "AM" && hour == 12)
                    hour = 0;
                
                return new TimeSpan(hour, minute, 0);
            }
            
            // صيغة 24 ساعة (مثل "10:00" أو "10:00:00")
            var parts24 = time.Split(':');
            var hours = int.Parse(parts24[0]);
            var minutes = int.Parse(parts24[1]);
            var seconds = parts24.Length > 2 ? int.Parse(parts24[2]) : 0;
            
            return new TimeSpan(hours, minutes, seconds);
        }

        /// <summary>
        /// تحويل الكيان إلى DTO
        /// </summary>
        private AppointmentResponseDto MapToDto(Appointment appointment)
        {
            var averageRating = 0.0;
            if (appointment.Lawyer.Reviews != null && appointment.Lawyer.Reviews.Any())
            {
                averageRating = appointment.Lawyer.Reviews.Average(r => r.Rating);
            }

            var dto = new AppointmentResponseDto
            {
                Id = appointment.Id,
                AppointmentNumber = appointment.AppointmentNumber,
                AppointmentType = appointment.AppointmentType,
                Date = appointment.Date,
                DateFormatted = appointment.Date.ToString("dd MMM yyyy"),
                Time = appointment.Time,
                DurationMinutes = appointment.DurationMinutes,
                Location = appointment.Location,
                Notes = appointment.Notes,
                Status = appointment.Status,
                RequestedAt = appointment.RequestedAt,
                ConfirmedAt = appointment.ConfirmedAt,
                IsUrgent = appointment.IsUrgent,
                User = new UserBriefDto
                {
                    Id = appointment.User.UserID,
                    FullName = appointment.User.FullName,
                    Email = appointment.User.Email,
                    PhoneNumber = appointment.User.Phone ?? ""
                },
                Lawyer = new LawyerBriefDto
                {
                    Id = appointment.Lawyer.UserId,
                    FullName = appointment.Lawyer.User?.FullName ?? "",
                    Specialization = appointment.Lawyer.Specialties?.FirstOrDefault()?.Specialty?.NameAr ?? "",
                    Rating = averageRating
                }
            };

            return dto;
        }

        #endregion
    }
}